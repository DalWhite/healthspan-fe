using System.Net;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Healthspan.Offline;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Universal training tracker through the offline mirror: intent (goal /
/// secondary goals / modality), blocks, attempts, quality intent, load type
/// and perceived fatigue must be stored locally, served back to the UI and
/// queued for sync with the payload keys the API's SyncService expects.
/// </summary>
public class UniversalTrackerOfflineTests
{
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static HttpClient ClientWithBase(MockHttpMessageHandler mockHttp)
    {
        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://localhost:7278/");
        return client;
    }

    private static (LocalMutator Mutator, InMemoryLocalStore Store, SyncStatusService Status)
        CreateMutator()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        return (new LocalMutator(store, status), store, status);
    }

    private static HttpRequestMessage Post(string path, object body)
        => new(HttpMethod.Post, $"https://localhost:7278{path}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, WireOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

    private static HttpRequestMessage Patch(string path, object body)
        => new(HttpMethod.Patch, $"https://localhost:7278{path}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, WireOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

    private static HttpRequestMessage Put(string path, object body)
        => new(HttpMethod.Put, $"https://localhost:7278{path}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, WireOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage? response)
    {
        response.Should().NotBeNull();
        var json = await response!.Content.ReadAsStringAsync();
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json).RootElement;
    }

    private static JsonElement Payload(SyncOperation operation)
        => JsonDocument.Parse(operation.Payload).RootElement;

    // ── Intent ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_WithTrackerIntent_StoresIntent_AndQueuesIt()
    {
        var (mutator, store, _) = CreateMutator();

        var created = await mutator.TryHandleAsync(Post("/workouts", new
        {
            date = new DateTime(2026, 10, 21, 12, 0, 0),
            type = "Training",
            goal = "Hypertrophy",
            secondaryGoals = new[] { "Strength" },
            modality = "Fuerza"
        }));

        created!.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await BodyAsync(created);
        body.GetProperty("goal").GetString().Should().Be("Hypertrophy");
        body.GetProperty("modality").GetString().Should().Be("Fuerza");
        body.GetProperty("secondaryGoals")[0].GetString().Should().Be("Strength");

        var queued = await store.PeekQueueAsync(10);
        var workoutOp = queued.Single(op => op.Entity == SyncEntities.Workout);
        workoutOp.Action.Should().Be(SyncActions.Create);
        Payload(workoutOp).GetProperty("goal").GetString().Should().Be("Hypertrophy");
        Payload(workoutOp).GetProperty("modality").GetString().Should().Be("Fuerza");
        Payload(workoutOp).GetProperty("secondaryGoals")[0].GetString().Should().Be("Strength");
    }

    [Fact]
    public async Task PatchWorkout_FatigueAndGoal_StoresAndQueuesUpdate()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        var response = await mutator.TryHandleAsync(Patch($"/workouts/{workoutId}", new
        {
            goal = "Strength",
            perceivedFatigue = 4
        }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await store.GetWorkoutAsync(workoutId);
        stored!.Goal.Should().Be("Strength");
        stored.PerceivedFatigue.Should().Be(4);

        var update = (await store.PeekQueueAsync(10)).Single(op =>
            op.Entity == SyncEntities.Workout && op.Action == SyncActions.Update);
        Payload(update).GetProperty("goal").GetString().Should().Be("Strength");
        Payload(update).GetProperty("perceivedFatigue").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task PatchWorkout_UnknownGoalOrFatigue_Returns400_AndKeepsTheSession()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        var badGoal = await mutator.TryHandleAsync(
            Patch($"/workouts/{workoutId}", new { goal = "Flexibility" }));
        badGoal!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BodyAsync(badGoal)).GetProperty("title").GetString()
            .Should().Contain("training goal");

        var badFatigue = await mutator.TryHandleAsync(
            Patch($"/workouts/{workoutId}", new { perceivedFatigue = 9 }));
        badFatigue!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BodyAsync(badFatigue)).GetProperty("title").GetString()
            .Should().Contain("between 1 and 5");

        var stored = await store.GetWorkoutAsync(workoutId);
        stored!.Goal.Should().BeNull();
        stored.PerceivedFatigue.Should().BeNull();
        (await store.PeekQueueAsync(10))
            .Should().NotContain(op => op.Action == SyncActions.Update);
    }

    // ── Blocks ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddBlock_ValidType_AppendsBlock_AndQueuesWorkoutBlockCreate()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/blocks", new
            {
                order = 0,
                type = "Superset",
                label = "Superserie A",
                rounds = 4
            }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await BodyAsync(response);
        var block = session.GetProperty("blocks").EnumerateArray().Single();
        block.GetProperty("type").GetString().Should().Be("Superset");
        block.GetProperty("label").GetString().Should().Be("Superserie A");
        block.GetProperty("rounds").GetInt32().Should().Be(4);

        var blockOp = (await store.PeekQueueAsync(10))
            .Single(op => op.Entity == SyncEntities.WorkoutBlock);
        blockOp.Action.Should().Be(SyncActions.Create);
        Payload(blockOp).GetProperty("type").GetString().Should().Be("Superset");
        Payload(blockOp).GetProperty("rounds").GetInt32().Should().Be(4);
        Payload(blockOp).GetProperty("workoutClientId").GetGuid().Should().Be(workoutId);
    }

    [Fact]
    public async Task AddBlock_InvalidType_Returns400_AndStoresNothing()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/blocks", new { type = "Marathon" }));

        response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BodyAsync(response)).GetProperty("title").GetString()
            .Should().Contain("block type");

        (await store.GetBlocksForWorkoutAsync(workoutId)).Should().BeEmpty();
        (await store.PeekQueueAsync(10))
            .Should().NotContain(op => op.Entity == SyncEntities.WorkoutBlock);
    }

    [Fact]
    public async Task DeleteBlock_KeepsExercisesAsImplicitNormalBlock()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);
        var blockId = await AddBlockAsync(mutator, workoutId, "Tabata");
        var exerciseId = await AddTrackerExerciseAsync(mutator, workoutId, blockId);

        var response = await mutator.TryHandleAsync(
            new(HttpMethod.Delete, $"https://localhost:7278/workouts/blocks/{blockId}"));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var exercise = await store.GetExerciseAsync(exerciseId);
        exercise!.BlockClientId.Should().BeNull();

        (await store.PeekQueueAsync(10)).Should().Contain(op =>
            op.Entity == SyncEntities.WorkoutBlock && op.Action == SyncActions.Delete);
    }

    // ── Attempts, quality intent, load type ───────────────────────────────────

    [Fact]
    public async Task AddExercise_WithBlockAndAttempts_LandsInBlock_AndPayloadCarriesIt()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);
        var blockId = await AddBlockAsync(mutator, workoutId, "Circuito", rounds: 3);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/exercises", new
            {
                typeId = 42,
                series = 0,
                repetitions = 0,
                weight = 0,
                weightType = "None",
                blockId,
                attempts = 8,
                completedAttempts = 0,
                qualityIntent = "Explosive",
                loadType = "Bodyweight"
            }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await BodyAsync(response);
        var exercise = session.GetProperty("exercises").EnumerateArray().Single();
        exercise.GetProperty("blockId").GetGuid().Should().Be(blockId);
        exercise.GetProperty("attempts").GetInt32().Should().Be(8);
        exercise.GetProperty("completedAttempts").GetInt32().Should().Be(0);
        exercise.GetProperty("qualityIntent").GetString().Should().Be("Explosive");
        exercise.GetProperty("loadType").GetString().Should().Be("Bodyweight");

        // The block groups its exercises in the session payload.
        var block = session.GetProperty("blocks").EnumerateArray().Single();
        block.GetProperty("exercises").EnumerateArray()
            .Single().GetProperty("id").GetGuid()
            .Should().Be(exercise.GetProperty("id").GetGuid());

        var exerciseOp = (await store.PeekQueueAsync(10))
            .Single(op => op.Entity == SyncEntities.Exercise);
        Payload(exerciseOp).GetProperty("blockClientId").GetGuid().Should().Be(blockId);
        Payload(exerciseOp).GetProperty("attempts").GetInt32().Should().Be(8);
        Payload(exerciseOp).GetProperty("qualityIntent").GetString().Should().Be("Explosive");
        Payload(exerciseOp).GetProperty("loadType").GetString().Should().Be("Bodyweight");
    }

    [Fact]
    public async Task CompleteAttempt_PersistsProgress_AndRequeuesTheExercise()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);
        var exerciseId = await AddTrackerExerciseAsync(
            mutator, workoutId, attempts: 8, completedAttempts: 0);

        var response = await mutator.TryHandleAsync(Put("/Exercises", new
        {
            id = exerciseId,
            series = 0,
            repetitions = 0,
            weight = 0,
            attempts = 8,
            completedAttempts = 3,
            qualityIntent = "Explosive",
            loadType = "Bodyweight"
        }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await store.GetExercisesForWorkoutAsync(workoutId)).Single();
        stored.CompletedAttempts.Should().Be(3);
        stored.Attempts.Should().Be(8);

        var update = (await store.PeekQueueAsync(10)).Single(op =>
            op.Entity == SyncEntities.Exercise && op.Action == SyncActions.Update);
        Payload(update).GetProperty("completedAttempts").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task CompleteAttempt_BeyondPlannedAttempts_GrowsThePlan()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);
        var exerciseId = await AddTrackerExerciseAsync(
            mutator, workoutId, attempts: 2, completedAttempts: 2);

        var response = await mutator.TryHandleAsync(Put("/Exercises", new
        {
            id = exerciseId,
            series = 0,
            repetitions = 0,
            weight = 0,
            attempts = 2,
            completedAttempts = 5
        }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = (await store.GetExercisesForWorkoutAsync(workoutId)).Single();
        stored.CompletedAttempts.Should().Be(5);
        stored.Attempts.Should().Be(5);
    }

    // ── Duplicate keeps the structure ─────────────────────────────────────────

    [Fact]
    public async Task DuplicateWorkout_ClonesBlocksAndRemapsExercises()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);
        var blockId = await AddBlockAsync(mutator, workoutId, "EMOM", label: "EMOM 12", rounds: 12);
        await AddTrackerExerciseAsync(mutator, workoutId, blockId);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/duplicate", new
            {
                date = "2026-10-22"
                }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var cloneId = (await BodyAsync(response))
            .GetProperty("workout").GetProperty("id").GetGuid();

        var cloneBlock = (await store.GetBlocksForWorkoutAsync(cloneId)).Single();
        cloneBlock.ClientId.Should().NotBe(blockId);
        cloneBlock.Type.Should().Be("EMOM");
        cloneBlock.Rounds.Should().Be(12);

        var cloneExercise = (await store.GetExercisesForWorkoutAsync(cloneId)).Single();
        cloneExercise.BlockClientId.Should().Be(cloneBlock.ClientId);
        cloneExercise.Attempts.Should().Be(8);
    }

    // ── Backward compatibility ────────────────────────────────────────────────

    [Fact]
    public async Task LegacySession_WithoutTrackerFields_ServesNoBlocks_AndDefaultsAttempts()
    {
        var (mutator, store, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/exercises", new
            {
                typeId = 7,
                series = 3,
                repetitions = 10,
                weight = 20,
                weightType = "External"
            }));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await BodyAsync(response);
        session.GetProperty("blocks").GetArrayLength().Should().Be(0);

        var exercise = session.GetProperty("exercises").EnumerateArray().Single();
        if (exercise.TryGetProperty("blockId", out var blockId))
        {
            blockId.ValueKind.Should().Be(JsonValueKind.Null);
        }
        exercise.GetProperty("attempts").GetInt32().Should().Be(1);
        exercise.GetProperty("completedAttempts").GetInt32().Should().Be(1);

        (await store.GetBlocksForWorkoutAsync(workoutId)).Should().BeEmpty();
        (await store.PeekQueueAsync(10))
            .Should().NotContain(op => op.Entity == SyncEntities.WorkoutBlock);
    }

    // ── Full offline cycle ────────────────────────────────────────────────────

    [Fact]
    public async Task OfflineTrackerCycle_QueuesWorkoutBlockAndExercise_AndDrainsThem()
    {
        var (mutator, store, status) = CreateMutator();
        status.MarkOffline();

        var created = await mutator.TryHandleAsync(Post("/workouts", new
        {
            date = new DateTime(2026, 10, 21, 12, 0, 0),
            type = "Training",
            goal = "Power",
            modality = "Pliometria"
        }));
        created!.StatusCode.Should().Be(HttpStatusCode.OK);
        var workoutId = (await BodyAsync(created)).GetProperty("id").GetGuid();

        var blockId = await AddBlockAsync(mutator, workoutId, "AMRAP", rounds: 20);
        await AddTrackerExerciseAsync(mutator, workoutId, blockId);

        var queued = await store.PeekQueueAsync(10);
        queued.Should().HaveCount(3);
        queued.Select(op => op.Entity).Should().ContainInOrder(
            SyncEntities.Workout, SyncEntities.WorkoutBlock, SyncEntities.Exercise);
        Payload(queued[1]).GetProperty("workoutClientId").GetGuid().Should().Be(workoutId);
        Payload(queued[2]).GetProperty("blockClientId").GetGuid().Should().Be(blockId);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "/sync")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                applied = 3,
                deduped = 0,
                failed = Array.Empty<object>(),
                idMap = Array.Empty<object>()
            }, WireOptions));

        var engine = new SyncEngine(() => ClientWithBase(mockHttp), store, status);
        var applied = await engine.DrainAsync();

        applied.Should().Be(3);
        (await store.CountPendingAsync()).Should().Be(0);
    }

    // ── route matching semantics (Regex → segment matcher) ───────────────────

    [Fact]
    public async Task Route_Matching_Is_Case_Insensitive_And_Segment_Exact()
    {
        var (mutator, _, _) = CreateMutator();
        var workoutId = await CreateWorkoutAsync(mutator);

        // "/Workouts/{id}/Blocks" must still hit the blocks route (the old
        // pattern matched against a lower-cased path).
        var upper = await mutator.TryHandleAsync(
            Post($"/Workouts/{workoutId}/Blocks", new { type = "Marathon" }));
        upper!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await BodyAsync(upper)).GetProperty("title").GetString()
            .Should().Contain("block type");

        // An extra segment must NOT match any route (null from the handler).
        (await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/blocks/extra", new { type = "Normal" })))
            .Should().BeNull();

        // An empty id segment must not match either.
        (await mutator.TryHandleAsync(Post("/workouts//blocks", new { type = "Normal" })))
            .Should().BeNull();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async Task<Guid> CreateWorkoutAsync(LocalMutator mutator)
    {
        var created = await mutator.TryHandleAsync(Post("/workouts", new
        {
            date = new DateTime(2026, 10, 21, 12, 0, 0),
            type = "Training"
        }));
        return (await BodyAsync(created)).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> AddBlockAsync(
        LocalMutator mutator, Guid workoutId, string type,
        string? label = null, int rounds = 1)
    {
        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/blocks", new { type, label, rounds }));
        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await BodyAsync(response))
            .GetProperty("blocks").EnumerateArray().Single()
            .GetProperty("id").GetGuid();
    }

    private static async Task<Guid> AddTrackerExerciseAsync(
        LocalMutator mutator,
        Guid workoutId,
        Guid? blockId = null,
        int attempts = 8,
        int completedAttempts = 0)
    {
        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutId}/exercises", new
            {
                typeId = 42,
                series = 0,
                repetitions = 0,
                weight = 0,
                weightType = "None",
                blockId,
                attempts,
                completedAttempts,
                qualityIntent = "Explosive",
                loadType = "Bodyweight"
            }));
        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await BodyAsync(response))
            .GetProperty("exercises").EnumerateArray().Single()
            .GetProperty("id").GetGuid();
    }
}
