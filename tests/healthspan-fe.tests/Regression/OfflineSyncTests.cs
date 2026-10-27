using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Healthspan.Offline;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Tests for the offline layer: local-first mutations against the in-memory
/// mirror, queue draining against POST /sync, and offline read behaviour.
/// InMemoryLocalStore mirrors the SQLite semantics, so these exercise the
/// exact code paths JsLocalStore runs in the browser.
/// </summary>
public class OfflineSyncTests
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

    // ── LocalMutator: local-first mutations ──────────────────────────────────

    [Fact]
    public async Task CreateWorkout_AppliesLocally_AndEnqueuesSync()
    {
        var (mutator, store, status) = CreateMutator();

        var response = await mutator.TryHandleAsync(
            Post("/workouts", new { date = new DateTime(2026, 10, 16, 12, 0, 0), type = "Fuerza" }));

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        payload.RootElement.TryGetProperty("id", out var clientId).Should().BeTrue();

        var workout = await store.GetWorkoutAsync(Guid.Parse(clientId.GetString()!));
        workout.Should().NotBeNull();
        workout!.Type.Should().Be("Fuerza");
        workout.Date.Should().Be(new DateOnly(2026, 10, 16));

        (await store.CountPendingAsync()).Should().Be(1);
        var queued = (await store.PeekQueueAsync(10)).Single();
        queued.Entity.Should().Be(SyncEntities.Workout);
        queued.Action.Should().Be(SyncActions.Create);
        status.PendingCount.Should().Be(1);
        status.CurrentStatus.Should().Be(SyncUiStatus.Pending);
    }

    [Fact]
    public async Task AddExerciseToWorkout_ResolvesLocalWorkoutByClientId()
    {
        var (mutator, store, _) = CreateMutator();

        var created = await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Push" }));
        var createdJson = JsonDocument.Parse(
            await created!.Content.ReadAsStringAsync());
        var workoutClientId = Guid.Parse(
            createdJson.RootElement.GetProperty("id").GetString()!);

        var response = await mutator.TryHandleAsync(
            Post($"/workouts/{workoutClientId}/exercises",
                new { typeId = 42, series = 3, repetitions = 10, weight = 80.5 }));

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        session.RootElement.GetProperty("exercises").GetArrayLength().Should().Be(1);
        session.RootElement.GetProperty("exercises")[0]
            .GetProperty("repetitions").GetDecimal().Should().Be(10);

        var exercises = await store.GetExercisesForWorkoutAsync(workoutClientId);
        exercises.Should().HaveCount(1);
        exercises[0].Series.Should().Be(3);
        exercises[0].Volume.Should().Be(3 * 10 * 80.5);

        (await store.CountPendingAsync()).Should().Be(2);
    }

    [Fact]
    public async Task DeleteWorkout_MarksMirrorDeleted_AndEnqueuesDelete()
    {
        var (mutator, store, _) = CreateMutator();

        var created = await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Legs" }));
        var clientId = Guid.Parse(JsonDocument
            .Parse(await created!.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!);

        var response = await mutator.TryHandleAsync(
            new(HttpMethod.Delete, $"https://localhost:7278/workouts/{clientId}"));

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var workout = await store.GetWorkoutAsync(clientId);
        workout.Should().NotBeNull();
        workout!.Deleted.Should().BeTrue();

        var queued = (await store.PeekQueueAsync(10))
            .Single(op => op.Action == SyncActions.Delete);
        queued.Entity.Should().Be(SyncEntities.Workout);
    }

    // ── SyncEngine: queue drain against POST /sync ───────────────────────────

    [Fact]
    public async Task SyncEngine_DrainsQueue_AndRemapsServerIds()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);

        var created = await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Core" }));
        var clientId = Guid.Parse(JsonDocument
            .Parse(await created!.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!);
        var serverId = Guid.NewGuid();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "/sync")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                applied = 1,
                deduped = 0,
                failed = Array.Empty<object>(),
                idMap = new[] { new { clientId, serverId } }
            }, WireOptions));

        var engine = new SyncEngine(
            () => ClientWithBase(mockHttp), store, status);

        var applied = await engine.DrainAsync();

        applied.Should().Be(1);
        (await store.CountPendingAsync()).Should().Be(0);

        var workout = await store.GetWorkoutAsync(clientId);
        workout!.ServerId.Should().Be(serverId);
    }

    [Fact]
    public async Task SyncEngine_KeepsQueue_WhenNetworkFails()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);

        await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Cardio" }));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "/sync")
            .Throw(new HttpRequestException("no network"));

        var engine = new SyncEngine(
            () => ClientWithBase(mockHttp), store, status);

        var applied = await engine.DrainAsync();

        applied.Should().Be(0);
        (await store.CountPendingAsync()).Should().Be(1);
        status.IsOnline.Should().BeFalse();
        status.CurrentStatus.Should().Be(SyncUiStatus.Offline);

        // The retry must be safe: the op keeps its OpId for server-side dedupe.
        var queued = (await store.PeekQueueAsync(10)).Single();
        queued.Attempts.Should().Be(1);
        queued.LastError.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SyncEngine_DropsServerRejectedOps_AfterMarkingThem()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);

        await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Bad" }));
        var opId = (await store.PeekQueueAsync(10)).Single().OpId;

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "/sync")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                applied = 0,
                deduped = 0,
                failed = new[] { new { opId, reason = "Validation failed." } },
                idMap = Array.Empty<object>()
            }, WireOptions));

        var engine = new SyncEngine(
            () => ClientWithBase(mockHttp), store, status);

        var applied = await engine.DrainAsync();

        applied.Should().Be(0);
        // Replaying a rejected op would fail forever: it leaves the queue.
        (await store.CountPendingAsync()).Should().Be(0);
    }

    // ── OfflineHttpHandler: offline reads and network-first GETs ─────────────

    [Fact]
    public async Task Handler_ServesLocalWorkouts_WhileOffline()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);
        var coordinator = new SyncCoordinator();
        var handler = new OfflineHttpHandler(store, mutator, status, coordinator);

        // Create while "online" (local-first, no network needed).
        await mutator.TryHandleAsync(
            Post("/workouts", new { date = DateTime.UtcNow, type = "Offline legs" }));

        status.MarkOffline();

        var mockHttp = new MockHttpMessageHandler();
        var response = await handler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            mockHttp,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        list.RootElement.GetArrayLength().Should().Be(1);
        list.RootElement[0].GetProperty("type").GetString().Should().Be("Offline legs");
    }

    [Fact]
    public async Task Handler_PrefersNetwork_ForGets_WhenNoLocalChanges()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);
        var coordinator = new SyncCoordinator();
        var handler = new OfflineHttpHandler(store, mutator, status, coordinator);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "/workouts")
            .Respond("application/json",
                "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"date\":\"2026-10-15T00:00:00\",\"type\":\"From server\"}]");

        var response = await handler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            mockHttp,
            CancellationToken.None);

        var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        list.RootElement[0].GetProperty("type").GetString().Should().Be("From server");

        // The successful GET was cached for offline reads. The write is detached
        // on purpose (t_710a4f65: the page that triggered the read must not wait
        // for the cache), so it has to be given a bounded moment to land.
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline
               && await store.GetCachedResponseAsync("workouts") is null)
        {
            await Task.Delay(20);
        }

        var cached = await store.GetCachedResponseAsync("workouts");
        cached.Should().NotBeNull();
    }

    [Fact]
    public async Task Handler_ServesCachedResponse_WhenNetworkDropsAfterCaching()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);
        var coordinator = new SyncCoordinator();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "/workouts")
            .Respond("application/json",
                "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"date\":\"2026-10-15T00:00:00\",\"type\":\"Cached type\"}]");

        var onlineHandler = new OfflineHttpHandler(store, mutator, status, coordinator);
        await onlineHandler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            mockHttp,
            CancellationToken.None);

        var offlineHttp = new MockHttpMessageHandler();
        offlineHttp.When(HttpMethod.Get, "/workouts")
            .Throw(new HttpRequestException("no network"));

        var offlineHandler = new OfflineHttpHandler(store, mutator, status, coordinator);
        var response = await offlineHandler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            offlineHttp,
            CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        list.RootElement[0].GetProperty("type").GetString().Should().Be("Cached type");
        status.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_ServesCachedResponse_WhenNetworkIsSlow_AndRefreshesInBackground()
    {
        var store = new InMemoryLocalStore();
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);
        var coordinator = new SyncCoordinator();

        // Seed the cache with a fast first read.
        var seedHttp = new MockHttpMessageHandler();
        seedHttp.When(HttpMethod.Get, "/workouts")
            .Respond("application/json",
                "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"date\":\"2026-10-15T00:00:00\",\"type\":\"Cached type\"}]");
        var seedHandler = new OfflineHttpHandler(store, mutator, status, coordinator);
        await seedHandler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            seedHttp,
            CancellationToken.None);

        // The seed's cache write is detached (t_710a4f65), so wait for it to land —
        // otherwise the next request sees no cache and blocks on the slow network.
        var seedDeadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < seedDeadline
               && await store.GetCachedResponseAsync("workouts") is null)
        {
            await Task.Delay(20);
        }

        // Now the network is alive but slow (8s > the 4s stale-while-revalidate budget).
        var slowHandler = new OfflineHttpHandler(store, mutator, status, coordinator);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await slowHandler.ProcessAsync(
            new(HttpMethod.Get, "https://localhost:7278/workouts"),
            new SlowNetworkHandler(TimeSpan.FromSeconds(8), "Fresh type"),
            CancellationToken.None);
        sw.Stop();

        // The user must not wait for the slow network: the cached copy is served.
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(6));
        var list = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        list.RootElement[0].GetProperty("type").GetString().Should().Be("Cached type");

        // The slow network still refreshed the cache in the background.
        var refreshDeadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < refreshDeadline
               && (await store.GetCachedResponseAsync("workouts"))?.Body?.Contains("Fresh type") != true)
        {
            await Task.Delay(50);
        }

        var cached = await store.GetCachedResponseAsync("workouts");
        cached.Should().NotBeNull();
        cached!.Body.Should().Contain("Fresh type");
    }

    private sealed class SlowNetworkHandler : HttpMessageHandler
    {
        private readonly TimeSpan delay;
        private readonly string bodyType;

        public SlowNetworkHandler(TimeSpan delay, string bodyType)
        {
            this.delay = delay;
            this.bodyType = bodyType;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"[{{\"id\":\"11111111-1111-1111-1111-111111111111\",\"date\":\"2026-10-15T00:00:00\",\"type\":\"{bodyType}\"}}]",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
