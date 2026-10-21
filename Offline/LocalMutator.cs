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

        // ── /Workouts/{id}/blocks ─────────────────────────────────────────────
        var blockAdd = Match(path, @"^/workouts/([^/]+)/blocks$");
        if (blockAdd is not null && method == HttpMethod.Post)
        {
            var workout = await ResolveWorkoutAsync(blockAdd);
            if (workout is null)
            {
                return Problem(HttpStatusCode.NotFound, "Workout not found.");
            }

            var body = await ReadJsonAsync<AddBlockBody>(request);
            if (body is null || WorkoutBlockTypeLabels.Parse(body.Type) is null)
            {
                return Problem(HttpStatusCode.BadRequest,
                    "Choose a block type: Normal, Superset, Circuito, EMOM, AMRAP, Tabata or Ronda.");
            }

            return JsonResponse(await AddBlockAsync(workout, body));
        }

        var blockById = Match(path, @"^/workouts/blocks/([^/]+)$");
        if (blockById is not null && method == HttpMethod.Delete)
        {
            var block = await ResolveBlockAsync(blockById);
            if (block is null)
            {
                return Problem(HttpStatusCode.NotFound, "Block not found.");
            }

            return await DeleteBlockAsync(block);
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

            if (method == HttpMethod.Patch)
            {
                var workout = await ResolveWorkoutAsync(workoutById);
                if (workout is null)
                {
                    return Problem(HttpStatusCode.NotFound, "Workout not found.");
                }

                var body = await ReadJsonAsync<PatchWorkoutBody>(request);
                return await PatchWorkoutAsync(workout, body);
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
            workout.ClientId, WorkoutCreatePayload(workout));
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
            Type = string.IsNullOrWhiteSpace(body?.Type) ? "Training" : body!.Type,
            Goal = body?.Goal,
            SecondaryGoals = body?.SecondaryGoals is null ? [] : [.. body.SecondaryGoals],
            Modality = body?.Modality
        };
        await _store.UpsertWorkoutAsync(workout);
        await EnqueueAsync(SyncEntities.Workout, SyncActions.Create,
            workout.ClientId, WorkoutCreatePayload(workout));
        // FE contract for POST /Workouts is GetWorkoutResource ({id,...}).
        return new
        {
            id = workout.ClientId,
            date = workout.Date,
            type = workout.Type,
            goal = workout.Goal,
            secondaryGoals = workout.SecondaryGoals,
            modality = workout.Modality
        };
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
            workout.ClientId, WorkoutUpdatePayload(workout));
        return await ServeWorkoutDetailAsync(workout);
    }

    /// <summary>PATCH /Workouts/{id} — refine the intent, or close the session
    /// with the perceived fatigue. Only the fields the user touched change.</summary>
    private async Task<HttpResponseMessage> PatchWorkoutAsync(
        LocalWorkout workout, PatchWorkoutBody? body)
    {
        if (body is null)
        {
            return Problem(HttpStatusCode.BadRequest, "Invalid workout payload.");
        }

        if (body.Goal is not null && TrainingIntent.ParseGoal(body.Goal) is null)
        {
            return Problem(HttpStatusCode.BadRequest, "Unknown training goal.");
        }

        if (body.Modality is not null && TrainingIntent.ParseModality(body.Modality) is null)
        {
            return Problem(HttpStatusCode.BadRequest, "Unknown training modality.");
        }

        if (body.PerceivedFatigue is not null && !PerceivedFatigue.IsValid(body.PerceivedFatigue))
        {
            return Problem(HttpStatusCode.BadRequest,
                "Perceived fatigue must be between 1 and 5.");
        }

        if (body.Goal is not null)
        {
            workout.Goal = body.Goal;
        }

        if (body.SecondaryGoals is not null)
        {
            workout.SecondaryGoals = [.. body.SecondaryGoals];
        }

        if (body.Modality is not null)
        {
            workout.Modality = body.Modality;
        }

        if (body.PerceivedFatigue is not null)
        {
            workout.PerceivedFatigue = body.PerceivedFatigue;
        }

        if (!string.IsNullOrWhiteSpace(body.Type))
        {
            workout.Type = body.Type!;
        }

        if (body.Date is not null)
        {
            workout.Date = DateOnly.FromDateTime(body.Date.Value);
        }

        workout.UpdatedAtUtc = DateTime.UtcNow;
        await _store.UpsertWorkoutAsync(workout);
        await EnqueueAsync(SyncEntities.Workout, SyncActions.Update,
            workout.ClientId, WorkoutUpdatePayload(workout));
        return await ServeWorkoutDetailAsync(workout);
    }

    private static object WorkoutCreatePayload(LocalWorkout workout) => new
    {
        date = workout.Date,
        type = workout.Type,
        goal = workout.Goal,
        secondaryGoals = workout.SecondaryGoals,
        modality = workout.Modality,
        perceivedFatigue = workout.PerceivedFatigue
    };

    private static object WorkoutUpdatePayload(LocalWorkout workout) => new
    {
        id = workout.ServerId ?? workout.ClientId,
        date = workout.Date,
        type = workout.Type,
        goal = workout.Goal,
        secondaryGoals = workout.SecondaryGoals,
        modality = workout.Modality,
        perceivedFatigue = workout.PerceivedFatigue
    };

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
            Type = string.IsNullOrWhiteSpace(type) ? source.Type : type,
            Goal = source.Goal,
            SecondaryGoals = [.. source.SecondaryGoals],
            Modality = source.Modality
        };
        await _store.UpsertWorkoutAsync(clone);

        // Blocks travel with the copy: a duplicated circuit stays a circuit.
        // Each clone gets a fresh client id, and exercises map through it.
        var blockIds = new Dictionary<Guid, Guid>();
        foreach (var block in await _store.GetBlocksForWorkoutAsync(source.ClientId))
        {
            var blockClone = new LocalBlock
            {
                ClientId = Guid.NewGuid(),
                WorkoutClientId = clone.ClientId,
                Order = block.Order,
                Type = block.Type,
                Label = block.Label,
                Rounds = block.Rounds
            };
            blockIds[block.ClientId] = blockClone.ClientId;
            await _store.UpsertBlockAsync(blockClone);
            await EnqueueAsync(SyncEntities.WorkoutBlock, SyncActions.Create,
                blockClone.ClientId, new
                {
                    workoutClientId = clone.ClientId,
                    order = blockClone.Order,
                    type = blockClone.Type,
                    label = blockClone.Label,
                    rounds = blockClone.Rounds
                });
        }

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
                Tags = [.. exercise.Tags],
                BlockClientId = exercise.BlockClientId is not null
                    ? blockIds.GetValueOrDefault(exercise.BlockClientId.Value)
                    : null,
                Order = exercise.Order,
                Attempts = exercise.Attempts,
                CompletedAttempts = exercise.CompletedAttempts,
                QualityIntent = exercise.QualityIntent,
                LoadType = exercise.LoadType
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
                goal = clone.Goal,
                secondaryGoals = clone.SecondaryGoals,
                modality = clone.Modality,
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
                    tags = c.Tags,
                    blockClientId = c.BlockClientId,
                    order = c.Order,
                    attempts = c.Attempts,
                    completedAttempts = c.CompletedAttempts,
                    qualityIntent = c.QualityIntent,
                    loadType = c.LoadType
                }).ToList()
            });

        return new
        {
            sourceWorkoutId = source.ServerId ?? source.ClientId,
            workout = await BuildGetWorkoutAsync(
                clone, await _store.GetExercisesForWorkoutAsync(clone.ClientId))
        };
    }

    // ── Exercises ──────────────────────────────────────────────────────────────

    private async Task AddExerciseAsync(LocalWorkout workout, AddExerciseBody body)
    {
        var exercise = await BuildExerciseAsync(workout.ClientId, body);
        await _store.UpsertExerciseAsync(exercise);
        await EnqueueAsync(SyncEntities.Exercise, SyncActions.Create,
            exercise.ClientId, await BuildExercisePayloadAsync(exercise));
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

        var exercise = await BuildExerciseAsync(workout.ClientId, body);
        await _store.UpsertExerciseAsync(exercise);
        await EnqueueAsync(SyncEntities.Exercise, SyncActions.Create,
            exercise.ClientId, await BuildExercisePayloadAsync(exercise));

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
            workout = await BuildGetWorkoutAsync(workout, [])
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

        // Completing an attempt is the tap that matters mid-session, so the
        // local mirror must accept it on its own.
        if (body.CompletedAttempts is >= 0)
        {
            exercise.CompletedAttempts = body.CompletedAttempts.Value;
            if (exercise.CompletedAttempts > exercise.Attempts)
            {
                exercise.Attempts = exercise.CompletedAttempts;
            }
        }

        if (body.Attempts is >= 1)
        {
            exercise.Attempts = Math.Max(body.Attempts.Value, exercise.CompletedAttempts);
        }

        if (body.QualityIntent is not null)
        {
            exercise.QualityIntent = string.IsNullOrWhiteSpace(body.QualityIntent)
                ? null
                : body.QualityIntent.Trim();
        }

        if (body.LoadType is not null)
        {
            exercise.LoadType = string.IsNullOrWhiteSpace(body.LoadType)
                ? null
                : body.LoadType.Trim();
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
                tags = exercise.Tags,
                attempts = exercise.Attempts,
                completedAttempts = exercise.CompletedAttempts,
                qualityIntent = exercise.QualityIntent,
                loadType = exercise.LoadType
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
            attempts = exercise.Attempts,
            completedAttempts = exercise.CompletedAttempts,
            qualityIntent = exercise.QualityIntent,
            loadType = exercise.LoadType,
            workout = workout is null
                ? null
                : await BuildGetWorkoutAsync(workout, [])
        });
    }

    private async Task<LocalExercise> BuildExerciseAsync(
        Guid workoutClientId, AddExerciseBody body)
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

        var block = await ResolveBlockForWorkoutAsync(workoutClientId, body.BlockId);

        // Order 0 means "append at the end" — the same default the API uses,
        // so the FE never has to manage sequence numbers to log an exercise.
        var order = body.Order > 0
            ? body.Order
            : (await _store.GetExercisesForWorkoutAsync(workoutClientId))
                .Count(e => e.BlockClientId == block?.ClientId) + 1;

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
            Tags = body.Tags ?? [],
            BlockClientId = block?.ClientId,
            Order = order,
            Attempts = body.Attempts is >= 1 ? body.Attempts.Value : 1,
            CompletedAttempts = body.CompletedAttempts is >= 0
                ? body.CompletedAttempts.Value
                : 1,
            QualityIntent = string.IsNullOrWhiteSpace(body.QualityIntent)
                ? null
                : body.QualityIntent.Trim(),
            LoadType = string.IsNullOrWhiteSpace(body.LoadType)
                ? null
                : body.LoadType.Trim()
        };
    }

    /// <summary>A block reference may arrive as a client id (offline-created)
    /// or a server id (already synced). An unknown id is not an error: the
    /// exercise simply lands in the implicit Normal block.</summary>
    private async Task<LocalBlock?> ResolveBlockForWorkoutAsync(
        Guid workoutClientId, Guid? id)
    {
        if (id is null)
        {
            return null;
        }

        var byClient = await _store.GetBlockAsync(id.Value);
        if (byClient is not null && byClient.WorkoutClientId == workoutClientId)
        {
            return byClient;
        }

        var blocks = await _store.GetBlocksForWorkoutAsync(workoutClientId);
        return blocks.FirstOrDefault(b => b.ServerId == id);
    }

    private async Task<LocalBlock?> ResolveBlockAsync(string id)
    {
        if (!Guid.TryParse(id, out var clientId))
        {
            return null;
        }

        var byClient = await _store.GetBlockAsync(clientId);
        if (byClient is not null)
        {
            return byClient;
        }

        var workouts = await _store.GetWorkoutsAsync();
        foreach (var workout in workouts)
        {
            var match = (await _store.GetBlocksForWorkoutAsync(workout.ClientId))
                .FirstOrDefault(b => b.ServerId == clientId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private async Task<object> AddBlockAsync(LocalWorkout workout, AddBlockBody body)
    {
        var blocks = await _store.GetBlocksForWorkoutAsync(workout.ClientId);
        var block = new LocalBlock
        {
            ClientId = body.ClientEntityId is not null && body.ClientEntityId.Value != Guid.Empty
                ? body.ClientEntityId.Value
                : Guid.NewGuid(),
            WorkoutClientId = workout.ClientId,
            Order = body.Order > 0 ? body.Order : blocks.Count + 1,
            Type = body.Type,
            Label = string.IsNullOrWhiteSpace(body.Label) ? null : body.Label.Trim(),
            Rounds = body.Rounds
        };

        await _store.UpsertBlockAsync(block);
        await EnqueueAsync(SyncEntities.WorkoutBlock, SyncActions.Create,
            block.ClientId, new
            {
                workoutClientId = workout.ClientId,
                workoutId = workout.ServerId,
                order = block.Order,
                type = block.Type,
                label = block.Label,
                rounds = block.Rounds
            });

        return await BuildSessionAsync(workout);
    }

    private async Task<HttpResponseMessage> DeleteBlockAsync(LocalBlock block)
    {
        await _store.MarkBlockDeletedAsync(block.ClientId);

        // The exercises do not disappear with the block: they fall back to the
        // implicit Normal block, exactly like the API does on delete.
        var exercises = await _store.GetExercisesForWorkoutAsync(block.WorkoutClientId);
        foreach (var exercise in exercises.Where(e => e.BlockClientId == block.ClientId))
        {
            exercise.BlockClientId = null;
            await _store.UpsertExerciseAsync(exercise);
        }

        await EnqueueAsync(SyncEntities.WorkoutBlock, SyncActions.Delete,
            block.ClientId, new { id = block.ServerId });

        var workout = await _store.GetWorkoutAsync(block.WorkoutClientId);
        return workout is null
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : await ServeWorkoutDetailAsync(workout);
    }

    private async Task<object> BuildExercisePayloadAsync(LocalExercise exercise)
    {
        // The parent may still be client-only (created offline, not yet
        // synced). Always send the client id; the server maps it to its
        // server id when the parent's create op lands in the same batch.
        var parent = await _store.GetWorkoutAsync(exercise.WorkoutClientId);
        var block = exercise.BlockClientId is not null
            ? await _store.GetBlockAsync(exercise.BlockClientId.Value)
            : null;
        return new
        {
            workoutClientId = exercise.WorkoutClientId,
            workoutId = parent?.ServerId,
            typeId = exercise.TypeId,
            series = exercise.Series,
            repetitions = exercise.Repetitions,
            weight = exercise.Weight,
            bodyWeight = exercise.BodyWeight,
            addedWeight = exercise.AddedWeight,
            comments = exercise.Comments,
            tags = exercise.Tags,
            blockClientId = exercise.BlockClientId,
            blockId = block?.ServerId,
            order = exercise.Order,
            attempts = exercise.Attempts,
            completedAttempts = exercise.CompletedAttempts,
            qualityIntent = exercise.QualityIntent,
            loadType = exercise.LoadType
        };
    }

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
            resources.Add(await BuildGetWorkoutAsync(workout, exercises));
        }

        var response = JsonResponse(resources);
        response.Headers.TryAddWithoutValidation("X-Total-Count", ordered.Count.ToString());
        return response;
    }

    private async Task<HttpResponseMessage> ServeWorkoutDetailAsync(LocalWorkout workout)
    {
        var exercises = await _store.GetExercisesForWorkoutAsync(workout.ClientId);
        return JsonResponse(await BuildGetWorkoutAsync(workout, exercises));
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
        var blocks = await _store.GetBlocksForWorkoutAsync(workout.ClientId);
        var blockWireIds = BlockWireIds(blocks);

        return new
        {
            id = workout.ServerId ?? workout.ClientId,
            date = workout.Date,
            type = workout.Type,
            goal = workout.Goal,
            secondaryGoals = workout.SecondaryGoals,
            modality = workout.Modality,
            perceivedFatigue = workout.PerceivedFatigue,
            exercises = exercises.Select(e => SessionExerciseWire(e, blockWireIds)).ToList(),
            blocks = blocks.Select(b => new
            {
                id = blockWireIds[b.ClientId],
                clientEntityId = b.ClientId,
                order = b.Order,
                type = b.Type,
                label = b.Label,
                rounds = b.Rounds,
                exercises = exercises
                    .Where(e => e.BlockClientId == b.ClientId)
                    .Select(e => SessionExerciseWire(e, blockWireIds))
                    .ToList()
            }).ToList(),
            recentExerciseTypeIds = await GetRecentExerciseTypeIdsAsync()
        };
    }

    /// <summary>The id the FE sees for a block. It is the server id once the
    /// block is synced, the client id while it only exists offline; the mutator
    /// accepts either when an exercise is assigned to it.</summary>
    private static Dictionary<Guid, object?> BlockWireIds(List<LocalBlock> blocks)
        => blocks.ToDictionary(b => b.ClientId, b => (object?)(b.ServerId ?? b.ClientId));

    private static object SessionExerciseWire(
        LocalExercise e, Dictionary<Guid, object?> blockWireIds)
        => new
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
            tags = e.Tags,
            blockId = e.BlockClientId is null
                ? null
                : blockWireIds.GetValueOrDefault(e.BlockClientId.Value),
            order = e.Order,
            attempts = e.Attempts,
            completedAttempts = e.CompletedAttempts,
            qualityIntent = e.QualityIntent,
            loadType = e.LoadType
        };

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

    private async Task<object> BuildGetWorkoutAsync(
        LocalWorkout workout, List<LocalExercise> exercises)
    {
        var blocks = await _store.GetBlocksForWorkoutAsync(workout.ClientId);
        var blockWireIds = BlockWireIds(blocks);

        return new
        {
            id = workout.ServerId ?? workout.ClientId,
            type = workout.Type,
            date = workout.Date.ToDateTime(new TimeOnly(12, 0)),
            exerciseCount = exercises.Count,
            totalVolume = exercises.Sum(e => e.Volume),
            leadingMuscles = Array.Empty<string>(),
            tags = exercises.SelectMany(e => e.Tags).Distinct().ToList(),
            goal = workout.Goal,
            secondaryGoals = workout.SecondaryGoals,
            modality = workout.Modality,
            perceivedFatigue = workout.PerceivedFatigue,
            exercises = exercises.Select(e => HistoryExerciseWire(e, blockWireIds)).ToList(),
            blocks = blocks.Select(b => new
            {
                id = blockWireIds[b.ClientId],
                clientEntityId = b.ClientId,
                order = b.Order,
                type = b.Type,
                label = b.Label,
                rounds = b.Rounds,
                exercises = exercises
                    .Where(e => e.BlockClientId == b.ClientId)
                    .Select(e => HistoryExerciseWire(e, blockWireIds))
                    .ToList()
            }).ToList()
        };
    }

    private static object HistoryExerciseWire(
        LocalExercise e, Dictionary<Guid, object?> blockWireIds)
        => new
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
            muscles = Array.Empty<string>(),
            blockId = e.BlockClientId is null
                ? null
                : blockWireIds.GetValueOrDefault(e.BlockClientId.Value),
            order = e.Order,
            attempts = e.Attempts,
            completedAttempts = e.CompletedAttempts,
            qualityIntent = e.QualityIntent,
            loadType = e.LoadType
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
        public string? Goal { get; set; }
        public List<string>? SecondaryGoals { get; set; }
        public string? Modality { get; set; }
    }

    private sealed class UpdateWorkoutBody
    {
        public string? Id { get; set; }
        public DateTime? Date { get; set; }
        public string? Type { get; set; }
    }

    private sealed class PatchWorkoutBody
    {
        public string? Type { get; set; }
        public DateTime? Date { get; set; }
        public string? Goal { get; set; }
        public List<string>? SecondaryGoals { get; set; }
        public string? Modality { get; set; }
        public int? PerceivedFatigue { get; set; }
    }

    private sealed class AddBlockBody
    {
        public Guid? ClientEntityId { get; set; }
        public int Order { get; set; }
        public string Type { get; set; } = "Normal";
        public string? Label { get; set; }
        public int? Rounds { get; set; }
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
        public Guid? BlockId { get; set; }
        public int Order { get; set; }
        public int? Attempts { get; set; }
        public int? CompletedAttempts { get; set; }
        public string? QualityIntent { get; set; }
        public string? LoadType { get; set; }
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
        public int? Attempts { get; set; }
        public int? CompletedAttempts { get; set; }
        public string? QualityIntent { get; set; }
        public string? LoadType { get; set; }
    }
}
