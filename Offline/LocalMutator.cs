using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Healthspan.Offline;

/// <summary>
/// Local-first request processor: applies workout/exercise mutations against
/// the SQLite mirror and answers with the exact JSON shape the pages expect,
/// enqueueing each mutation for later replay against the API. Returns null
/// when the request is not one it owns (the handler then tries the network
/// or the response cache).
/// </summary>
public sealed class LocalMutator
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization
            .JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILocalStore _store;
    private readonly SyncStatusService _status;

    public LocalMutator(ILocalStore store, SyncStatusService status)
    {
        _store = store;
        _status = status;
    }

    public async Task<HttpResponseMessage?> TryHandleAsync(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath?.TrimEnd('/') ?? "";
        var query = HttpUtility.ParseQueryString(
            request.RequestUri?.Query ?? "");
        var method = request.Method;

        // ── Exercise catalog (read-only mirror) ───────────────────────────────
        if (method == HttpMethod.Get && path.Equals("/exercisetypes",
                StringComparison.OrdinalIgnoreCase))
        {
            return await ServeCatalogAsync("exercise_types", request);
        }

        if (method == HttpMethod.Get && path.Equals("/api/muscles",
                StringComparison.OrdinalIgnoreCase))
        {
            return await ServeCatalogAsync("muscles", request);
        }

        // ── Today flow ────────────────────────────────────────────────────────
        if (path.Equals("/workouts/today/sessions", StringComparison.OrdinalIgnoreCase)
            && method == HttpMethod.Get)
        {
            var date = ParseDate(query["date"]) ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return JsonResponse(await BuildTodaySessionsAsync(date));
        }

        if (path.Equals("/workouts/today", StringComparison.OrdinalIgnoreCase)
            && method == HttpMethod.Post)
        {
            var body = await ReadJsonAsync<StartSessionBody>(request);
            var date = body?.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var session = await StartTodaySessionAsync(date, body?.CreateNew ?? false);
            // The FE contract is WorkoutSessionResource ({id,...}); returning
            // the raw LocalWorkout ({clientId,...}) left the FE with Id=Empty.
            return JsonResponse(await BuildSessionAsync(session));
        }

        // ── /Workouts/{id}/... ────────────────────────────────────────────────
        var sessionAdd = Match(path, @"^/workouts/([^/]+)/exercises$");
        if (sessionAdd is not null && method == HttpMethod.Post)
        {
            var workout = await ResolveWorkoutAsync(sessionAdd);
            if (workout is null)
            {
                return Problem(HttpStatusCode.NotFound, "Workout not found.");
            }

            var body = await ReadJsonAsync<AddExerciseBody>(request);
            if (body is null)
            {
                return Problem(HttpStatusCode.BadRequest, "Invalid exercise payload.");
            }

            await AddExerciseAsync(workout, body);
            return JsonResponse(await BuildSessionAsync(workout));
        }

        var duplicate = Match(path, @"^/workouts/([^/]+)/duplicate$");
        if (duplicate is not null && method == HttpMethod.Post)
        {
            var source = await ResolveWorkoutAsync(duplicate);
            if (source is null)
            {
                return Problem(HttpStatusCode.NotFound, "Workout not found.");
            }

            var body = await ReadJsonAsync<DuplicateBody>(request);
            if (body is null || body.Date is null)
            {
                return Problem(HttpStatusCode.BadRequest, "Date is required.");
            }

            return JsonResponse(await DuplicateWorkoutAsync(
                source, body.Date.Value, body.Type, body.ExerciseIds));
        }

        // ── /Workouts CRUD ────────────────────────────────────────────────────
        if (path.Equals("/workouts", StringComparison.OrdinalIgnoreCase))
        {
            if (method == HttpMethod.Get)
            {
                return await ServeWorkoutListAsync(query);
            }

            if (method == HttpMethod.Post)
            {
                var body = await ReadJsonAsync<AddWorkoutBody>(request);
                return JsonResponse(await AddWorkoutAsync(body));

            }

            if (method == HttpMethod.Put)
            {
                var body = await ReadJsonAsync<UpdateWorkoutBody>(request);
                return await UpdateWorkoutAsync(body);
            }
        }

        var workoutById = Match(path, @"^/workouts/([^/]+)$");
        if (workoutById is not null)
        {
            if (method == HttpMethod.Get)
            {
                var workout = await ResolveWorkoutAsync(workoutById);
                return workout is null
                    ? Problem(HttpStatusCode.NotFound, "Workout not found.")
                    : await ServeWorkoutDetailAsync(workout);
            }

            if (method == HttpMethod.Delete)
            {
                var workout = await ResolveWorkoutAsync(workoutById);
                if (workout is null)
                {
                    return Problem(HttpStatusCode.NotFound, "Workout not found.");
                }

                await _store.MarkWorkoutDeletedAsync(workout.ClientId);
                await _store.MarkWorkoutExercisesDeletedAsync(workout.ClientId);
                await EnqueueAsync(SyncEntities.Workout, SyncActions.Delete,
                    workout.ClientId, new { id = workout.ServerId });
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
        }

        // ── /Exercises CRUD ───────────────────────────────────────────────────
        if (path.Equals("/exercises", StringComparison.OrdinalIgnoreCase))
        {
            if (method == HttpMethod.Post)
            {
                var body = await ReadJsonAsync<AddExerciseBody>(request);
                if (body is null)
                {
                    return Problem(HttpStatusCode.BadRequest, "Invalid exercise payload.");
                }

                return await AddExerciseStandaloneAsync(body);
            }

            if (method == HttpMethod.Put)
            {
                var body = await ReadJsonAsync<UpdateExerciseBody>(request);
                return await UpdateExerciseAsync(body, query["clientExerciseId"]);
            }
        }

        var exerciseById = Match(path, @"^/exercises/([^/]+)$");
        if (exerciseById is not null && method == HttpMethod.Delete)
        {
            var exercise = await ResolveExerciseAsync(exerciseById);
            if (exercise is null)
            {
                return Problem(HttpStatusCode.NotFound, "Exercise not found.");
            }

            await _store.MarkExerciseDeletedAsync(exercise.ClientId);
            await EnqueueAsync(SyncEntities.Exercise, SyncActions.Delete,
                exercise.ClientId, new { id = exercise.ServerId });
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        return null;
    }

    // ── Workouts ───────────────────────────────────────────────────────────────

    private async Task<LocalWorkout> StartTodaySessionAsync(DateOnly date, bool createNew)
    {
        var existing = (await _store.GetWorkoutsAsync(date: date)).LastOrDefault();
        if (existing is not null && !createNew)
        {
            return existing;
        }

        var workout = new LocalWorkout
        {
            ClientId = Guid.NewGuid(),
            Date = date,
            Type = "Training"
        };
        await _store.UpsertWorkoutAsync(workout);
        await EnqueueAsync(SyncEntities.Workout, SyncActions.Create,
            workout.ClientId, new { date, type = workout.Type });
        return workout;
    }

    private async Task<object> AddWorkoutAsync(AddWorkoutBody? body)
    {
        var date = body?.Date is null
            ? DateOnly.FromDateTime(DateTime.UtcNow)
            : DateOnly.FromDateTime(body.Date.Value);
        var workout = new LocalWorkout
        {
            ClientId = Guid.NewGuid(),
            Date = date,
            Type = string.IsNullOrWhiteSpace(body?.Type) ? "Training" : body!.Type
        };
        await _store.UpsertWorkoutAsync(workout);
        await EnqueueAsync(SyncEntities.Workout, SyncActions.Create,
            workout.ClientId, new { date, type = workout.Type });
        // FE contract for POST /Workouts is GetWorkoutResource ({id,...}).
        return new { id = workout.ClientId, date = workout.Date, type = workout.Type };
    }

    private async Task<HttpResponseMessage> UpdateWorkoutAsync(UpdateWorkoutBody? body)
    {
        var workout = body?.Id is not null
            ? await ResolveWorkoutAsync(body.Id)
            : null;
        if (workout is null)
        {
            return Problem(HttpStatusCode.NotFound, "Workout not found.");
        }

        workout.Type = string.IsNullOrWhiteSpace(body!.Type) ? workout.Type : body.Type;
        if (body.Date is not null)
        {
            workout.Date = DateOnly.FromDateTime(body.Date.Value);
        }

        workout.UpdatedAtUtc = DateTime.UtcNow;
        await _store.UpsertWorkoutAsync(workout);
        await EnqueueAsync(SyncEntities.Workout, SyncActions.Update,
            workout.ClientId,
            new { id = workout.ServerId, date = workout.Date, type = workout.Type });
        return await ServeWorkoutDetailAsync(workout);
    }

    private async Task<object> DuplicateWorkoutAsync(
        LocalWorkout source,
        DateOnly targetDate,
        string? type,
        List<Guid>? exerciseIds)
    {
        var sourceExercises = await _store.GetExercisesForWorkoutAsync(source.ClientId);
        var selected = exerciseIds is { Count: > 0 }
            ? sourceExercises.Where(e => exerciseIds.Contains(e.ClientId)
                                         || (e.ServerId.HasValue && exerciseIds.Contains(e.ServerId.Value)))
                             .ToList()
            : sourceExercises;

        var clone = new LocalWorkout
        {
            ClientId = Guid.NewGuid(),
            Date = targetDate,
            Type = string.IsNullOrWhiteSpace(type) ? source.Type : type
        };
        await _store.UpsertWorkoutAsync(clone);

        var clones = new List<LocalExercise>();
        foreach (var exercise in selected)
        {
            var copy = new LocalExercise
            {
                ClientId = Guid.NewGuid(),
                WorkoutClientId = clone.ClientId,
                TypeId = exercise.TypeId,
                Name = exercise.Name,
                Series = exercise.Series,
                Repetitions = exercise.Repetitions,
                Weight = exercise.Weight,
                BodyWeight = exercise.BodyWeight,
                AddedWeight = exercise.AddedWeight,
                Comments = exercise.Comments,
                Tags = [.. exercise.Tags]
            };
            clones.Add(copy);
            await _store.UpsertExerciseAsync(copy);
        }

        await EnqueueAsync(SyncEntities.Workout, SyncActions.Duplicate,
            clone.ClientId,
            new
            {
                sourceWorkoutId = source.ServerId,
                date = targetDate,
                type = clone.Type,
                exercises = selected.Select(e => e.ServerId).ToList(),
                clonedExercises = clones.Select(c => new
                {
                    clientId = c.ClientId,
                    typeId = c.TypeId,
                    series = c.Series,
                    repetitions = c.Repetitions,
                    weight = c.Weight,
                    bodyWeight = c.BodyWeight,
                    addedWeight = c.AddedWeight,
                    comments = c.Comments,
                    tags = c.Tags
                }).ToList()
            });

        return new
        {
            sourceWorkoutId = source.ServerId ?? source.ClientId,
            workout = BuildGetWorkout(clone, await _store.GetExercisesForWorkoutAsync(clone.ClientId))
        };
    }

    // ── Exercises ──────────────────────────────────────────────────────────────

    private async Task AddExerciseAsync(LocalWorkout workout, AddExerciseBody body)
    {
        var exercise = BuildExercise(workout.ClientId, body);
        await _store.UpsertExerciseAsync(exercise);
        await EnqueueAsync(SyncEntities.Exercise, SyncActions.Create,
            exercise.ClientId, BuildExercisePayload(workout, exercise));
    }

    private async Task<HttpResponseMessage> AddExerciseStandaloneAsync(AddExerciseBody body)
    {
        var workout = body.WorkoutId is not null
            ? await ResolveWorkoutAsync(body.WorkoutId)
            : null;

        if (workout is null)
        {
            // Same rule as the API's Today flow: the workout appears the
            // moment the first exercise is logged, never as an empty day.
            workout = await StartTodaySessionAsync(
                DateOnly.FromDateTime(DateTime.UtcNow), createNew: false);
        }

        var exercise = BuildExercise(workout.ClientId, body);
        await _store.UpsertExerciseAsync(exercise);
        await EnqueueAsync(SyncEntities.Exercise, SyncActions.Create,
            exercise.ClientId, BuildExercisePayload(workout, exercise));

        return JsonResponse(new
        {
            id = exercise.ServerId ?? exercise.ClientId,
            series = exercise.Series,
            repetitions = exercise.Repetitions,
            weight = exercise.Weight,
            comments = exercise.Comments,
            tags = exercise.Tags,
            bodyWeight = exercise.BodyWeight,
            addedWeight = exercise.AddedWeight,
            workout = BuildGetWorkout(workout, [])
        });
    }

    private async Task<HttpResponseMessage> UpdateExerciseAsync(
        UpdateExerciseBody? body,
        string? clientExerciseId)
    {
        var exercise = body?.Id is not null
            ? await ResolveExerciseAsync(body.Id)
            : null;

        if (exercise is null && Guid.TryParse(clientExerciseId, out var fallbackId))
        {
            exercise = await _store.GetExerciseAsync(fallbackId);
        }

        if (exercise is null)
        {
            return Problem(HttpStatusCode.NotFound, "Exercise not found.");
        }

        exercise.Series = body!.Series ?? exercise.Series;
        exercise.Repetitions = body.Repetitions ?? exercise.Repetitions;
        exercise.Weight = body.Weight ?? exercise.Weight;
        exercise.BodyWeight = body.BodyWeight ?? exercise.BodyWeight;
        exercise.AddedWeight = body.AddedWeight ?? exercise.AddedWeight;
        exercise.Comments = body.Comments ?? exercise.Comments;
        if (body.Tags is { Count: > 0 })
        {
            exercise.Tags = body.Tags;
        }

        exercise.UpdatedAtUtc = DateTime.UtcNow;
        await _store.UpsertExerciseAsync(exercise);
        await EnqueueAsync(SyncEntities.Exercise, SyncActions.Update,
            exercise.ClientId,
            new
            {
                id = exercise.ServerId,
                series = exercise.Series,
                repetitions = exercise.Repetitions,
                weight = exercise.Weight,
                bodyWeight = exercise.BodyWeight,
                addedWeight = exercise.AddedWeight,
                comments = exercise.Comments,
                tags = exercise.Tags
            });

        var workout = await _store.GetWorkoutAsync(exercise.WorkoutClientId);
        return JsonResponse(new
        {
            id = exercise.ServerId ?? exercise.ClientId,
            series = exercise.Series,
            repetitions = exercise.Repetitions,
            weight = exercise.Weight,
            comments = exercise.Comments,
            tags = exercise.Tags,
            bodyWeight = exercise.BodyWeight,
            addedWeight = exercise.AddedWeight,
            workout = workout is null ? null : BuildGetWorkout(workout, [])
        });
    }

    private static LocalExercise BuildExercise(Guid workoutClientId, AddExerciseBody body)
    {
        double? bodyWeight = body.BodyWeight;
        double? addedWeight = body.AddedWeight;
        double? weight = body.Weight;

        // Mirror the FE's weight-type semantics so the local mirror matches
        // what the API will store after sync.
        switch (body.WeightType)
        {
            case "BodyWeight":
                bodyWeight = weight;
                weight = 0;
                break;
            case "AddedWeight":
                addedWeight = weight;
                weight = 0;
                break;
            case "None":
                weight = 0;
                break;
        }

        return new LocalExercise
        {
            ClientId = Guid.NewGuid(),
            WorkoutClientId = workoutClientId,
            TypeId = body.TypeId ?? 0,
            Series = body.Series,
            Repetitions = body.Repetitions,
            Weight = weight,
            BodyWeight = bodyWeight,
            AddedWeight = addedWeight,
            Comments = body.Comments,
            Tags = body.Tags ?? []
        };
    }

    private static object BuildExercisePayload(LocalWorkout workout, LocalExercise exercise)
        => new
        {
            workoutClientId = workout.ClientId,
            workoutId = workout.ServerId,
            typeId = exercise.TypeId,
            series = exercise.Series,
            repetitions = exercise.Repetitions,
            weight = exercise.Weight,
            bodyWeight = exercise.BodyWeight,
            addedWeight = exercise.AddedWeight,
            comments = exercise.Comments,
            tags = exercise.Tags
        };

    // ── Read paths ────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> ServeWorkoutListAsync(
        System.Collections.Specialized.NameValueCollection query)
    {
        var page = ParseInt(query["page"]) ?? 1;
        var pageSize = ParseInt(query["pageSize"]) ?? 20;
        var from = ParseDate(query["from"]);
        var to = ParseDate(query["to"]);
        var search = (query["search"] ?? "").Trim();
        var tag = (query["tag"] ?? "").Trim();

        var all = await _store.GetWorkoutsAsync();
        IEnumerable<LocalWorkout> filtered = all;
        if (from.HasValue)
        {
            filtered = filtered.Where(w => w.Date >= from.Value);
        }

        if (to.HasValue)
        {
            filtered = filtered.Where(w => w.Date <= to.Value);
        }

        if (search.Length > 0)
        {
            filtered = filtered.Where(w =>
                w.Type.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (tag.Length > 0)
        {
            var filteredWorkouts = new List<LocalWorkout>();
            foreach (var workout in filtered)
            {
                var exercises = await _store.GetExercisesForWorkoutAsync(workout.ClientId);
                if (exercises.Any(e => e.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                {
                    filteredWorkouts.Add(workout);
                }
            }

            filtered = filteredWorkouts;
        }

        var ordered = filtered.OrderByDescending(w => w.Date).ToList();
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var resources = new List<object>();
        foreach (var workout in pageItems)
        {
            var exercises = await _store.GetExercisesForWorkoutAsync(workout.ClientId);
            resources.Add(BuildGetWorkout(workout, exercises));
        }

        var response = JsonResponse(resources);
        response.Headers.TryAddWithoutValidation("X-Total-Count", ordered.Count.ToString());
        return response;
    }

    private async Task<HttpResponseMessage> ServeWorkoutDetailAsync(LocalWorkout workout)
    {
        var exercises = await _store.GetExercisesForWorkoutAsync(workout.ClientId);
        return JsonResponse(BuildGetWorkout(workout, exercises));
    }

    private async Task<List<object>> BuildTodaySessionsAsync(DateOnly date)
    {
        var sessions = new List<object>();
        foreach (var workout in await _store.GetWorkoutsAsync(date: date))
        {
            sessions.Add(await BuildSessionAsync(workout));
        }

        return sessions;
    }

    private async Task<object> BuildSessionAsync(LocalWorkout workout)
    {
        var exercises = await _store.GetExercisesForWorkoutAsync(workout.ClientId);
        return new
        {
            id = workout.ServerId ?? workout.ClientId,
            date = workout.Date,
            type = workout.Type,
            exercises = exercises.Select(e => new
            {
                id = e.ServerId ?? e.ClientId,
                typeId = e.TypeId,
                name = e.Name,
                series = e.Series,
                repetitions = e.Repetitions,
                weight = e.Weight,
                bodyWeight = e.BodyWeight,
                addedWeight = e.AddedWeight,
                comments = e.Comments,
                tags = e.Tags
            }).ToList(),
            recentExerciseTypeIds = await GetRecentExerciseTypeIdsAsync()
        };
    }

    private async Task<List<int>> GetRecentExerciseTypeIdsAsync()
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var typeIds = new List<int>();
        foreach (var workout in await _store.GetWorkoutsAsync())
        {
            if (workout.Date < cutoff)
            {
                continue;
            }

            foreach (var exercise in await _store.GetExercisesForWorkoutAsync(workout.ClientId))
            {
                typeIds.Add(exercise.TypeId);
            }
        }

        return typeIds.GroupBy(id => id)
                      .OrderByDescending(group => group.Count())
                      .Select(group => group.Key)
                      .Take(10)
                      .ToList();
    }

    private object BuildGetWorkout(LocalWorkout workout, List<LocalExercise> exercises)
        => new
        {
            id = workout.ServerId ?? workout.ClientId,
            type = workout.Type,
            date = workout.Date.ToDateTime(new TimeOnly(12, 0)),
            exerciseCount = exercises.Count,
            totalVolume = exercises.Sum(e => e.Volume),
            leadingMuscles = Array.Empty<string>(),
            tags = exercises.SelectMany(e => e.Tags).Distinct().ToList(),
            exercises = exercises.Select(e => new
            {
                id = e.ServerId ?? e.ClientId,
                typeId = e.TypeId,
                name = e.Name,
                series = e.Series,
                repetitions = e.Repetitions,
                weight = e.Weight,
                bodyWeight = e.BodyWeight,
                addedWeight = e.AddedWeight,
                volume = e.Volume,
                comments = e.Comments,
                tags = e.Tags,
                muscles = Array.Empty<string>()
            }).ToList()
        };

    private async Task<HttpResponseMessage> ServeCatalogAsync(
        string kind, HttpRequestMessage request)
    {
        if (await _store.HasCatalogAsync(kind))
        {
            var payloads = await _store.GetCatalogPayloadsAsync(kind);
            var items = payloads
                .Select(payload => JsonDocument.Parse(payload).RootElement.Clone())
                .ToList();
            return JsonResponse(items);
        }

        // No mirror yet: let the handler hit the network (and cache it).
        return null!;
    }

    // ── Resolution helpers ────────────────────────────────────────────────────

    private async Task<LocalWorkout?> ResolveWorkoutAsync(string? id)
    {
        if (id is null || !Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var workout = await _store.GetWorkoutAsync(guid);
        if (workout is not null)
        {
            return workout;
        }

        return (await _store.GetWorkoutsAsync(includeDeleted: true))
            .FirstOrDefault(w => w.ServerId == guid);
    }

    private async Task<LocalExercise?> ResolveExerciseAsync(string? id)
    {
        if (id is null || !Guid.TryParse(id, out var guid))
        {
            return null;
        }

        var exercise = await _store.GetExerciseAsync(guid);
        if (exercise is not null)
        {
            return exercise;
        }

        foreach (var workout in await _store.GetWorkoutsAsync(includeDeleted: true))
        {
            var match = (await _store.GetExercisesForWorkoutAsync(
                workout.ClientId, includeDeleted: true))
                .FirstOrDefault(e => e.ServerId == guid);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private async Task EnqueueAsync(
        string entity, string action, Guid clientEntityId, object payload)
    {
        await _store.EnqueueAsync(new SyncOperation
        {
            Entity = entity,
            Action = action,
            ClientEntityId = clientEntityId,
            Payload = JsonSerializer.Serialize(payload, JsonOptions)
        });

        // Stale cached GETs must not shadow the local mutation.
        await _store.InvalidateCacheAsync("workouts");
        await _store.InvalidateCacheAsync("exercises");
        _status.SetPendingCount(await _store.CountPendingAsync());
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────

    private static async Task<T?> ReadJsonAsync<T>(HttpRequestMessage request)
        where T : class
    {
        if (request.Content is null)
        {
            return null;
        }

        try
        {
            var json = await request.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static HttpResponseMessage JsonResponse(object payload)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

    private static HttpResponseMessage Problem(HttpStatusCode status, string title)
        => new(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { title, status = (int)status }, JsonOptions),
                Encoding.UTF8,
                "application/problem+json")
        };

    private static string? Match(string path, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            path.ToLowerInvariant(), pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var date) ? date : null;

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    // Payload shapes (mirroring the FE resources the pages post)

    private sealed class StartSessionBody
    {
        public DateOnly Date { get; set; }
        public string Type { get; set; } = "Training";
        public bool CreateNew { get; set; }
    }

    private sealed class AddWorkoutBody
    {
        public DateTime? Date { get; set; }
        public string? Type { get; set; }
    }

    private sealed class UpdateWorkoutBody
    {
        public string? Id { get; set; }
        public DateTime? Date { get; set; }
        public string? Type { get; set; }
    }

    private sealed class DuplicateBody
    {
        public DateOnly? Date { get; set; }
        public string? Type { get; set; }
        public List<Guid>? ExerciseIds { get; set; }
    }

    private sealed class AddExerciseBody
    {
        public string? WorkoutId { get; set; }
        public int? TypeId { get; set; }
        public int? Series { get; set; }
        public int? Repetitions { get; set; }
        public double? Weight { get; set; }
        public string? WeightType { get; set; }
        public double? BodyWeight { get; set; }
        public double? AddedWeight { get; set; }
        public string? Comments { get; set; }
        public List<string>? Tags { get; set; }
    }

    private sealed class UpdateExerciseBody
    {
        public string? Id { get; set; }
        public int? Series { get; set; }
        public int? Repetitions { get; set; }
        public double? Weight { get; set; }
        public double? BodyWeight { get; set; }
        public double? AddedWeight { get; set; }
        public string? Comments { get; set; }
        public List<string>? Tags { get; set; }
    }
}
