using Microsoft.Extensions.Logging;

namespace Healthspan.Offline;

/// <summary>
/// Offline-first DelegatingHandler for the training endpoints:
/// - mutations (POST/PUT/DELETE) are applied to the local SQLite mirror and
///   answered locally, then queued for replay against the API;
/// - GETs are served from the local mirror when it owns the data, from the
///   response cache when offline, or from the network (caching the response)
///   when online.
/// </summary>
public sealed class OfflineHttpHandler : DelegatingHandler
{
    private static readonly string[] CacheablePrefixes =
    [
        "workouts", "exercises", "exercisetypes", "api/muscles", "users",
        // Derived training reads: cached so the dashboard radar and the
        // readiness card still render offline or on a slow network.
        "adaptations", "readiness"
    ];

    private static readonly string[] MirrorPrefixes =
    [
        "workouts", "exercises"
    ];

    /// <summary>
    /// Stale-while-revalidate budget: a cached GET answers immediately if
    /// the network has not replied within this window; the network then
    /// refreshes the cache in the background.
    /// </summary>
    private static readonly TimeSpan SlowNetworkBudget = TimeSpan.FromSeconds(4);

    private readonly SemaphoreSlim _catalogMirrorLock = new(1, 1);
    private readonly ILocalStore _store;
    private readonly LocalMutator _mutator;
    private readonly SyncStatusService _status;
    private readonly SyncCoordinator _coordinator;
    private readonly ILogger<OfflineHttpHandler>? _logger;
    private HttpClient? _forward;

    public OfflineHttpHandler(
        ILocalStore store,
        LocalMutator mutator,
        SyncStatusService status,
        SyncCoordinator coordinator,
        ILogger<OfflineHttpHandler>? logger = null)
    {
        _store = store;
        _mutator = mutator;
        _status = status;
        _coordinator = coordinator;
        _logger = logger;
    }

    public Task<HttpResponseMessage> ProcessAsync(
        HttpRequestMessage request,
        HttpMessageHandler terminal,
        CancellationToken cancellationToken)
    {
        // This handler is never inserted directly into an HttpClient chain,
        // so it owns a private client whose only job is to forward to the
        // caller's real transport (a handler instance may belong to one chain).
        _forward ??= new HttpClient(new PassThroughHandler(terminal));
        return SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Chain mode: assigns the terminal transport (an HttpClientHandler on
    /// WASM) so SendNetworkAsync can delegate through the chain.
    /// </summary>
    public void AssignTerminalHandler(HttpMessageHandler terminal)
        => InnerHandler = terminal;

    private Task<HttpResponseMessage> SendNetworkAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        // ProcessAsync mode: reach the caller's transport through the private
        // forward client. Chain mode (InnerHandler assigned): normal delegation.
        => _forward is not null
            ? _forward.SendAsync(request, cancellationToken)
            : base.SendAsync(request, cancellationToken);

    /// <summary>
    /// Local mirror attempt that can never break the user's action: a mirror
    /// failure (schema drift, a broken OPFS file, a worker that died) falls
    /// back to the network path instead of throwing out of the handler, which
    /// would leave the page with no response and no error message.
    /// </summary>
    private async Task<HttpResponseMessage?> TryLocalAsync(HttpRequestMessage request)
    {
        try
        {
            return await _mutator.TryHandleAsync(request);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                ex,
                "Local mirror could not handle {Method} {Path}; falling back to the network.",
                request.Method.Method,
                request.RequestUri?.AbsolutePath);

            return null;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var isGet = request.Method == HttpMethod.Get;
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isTraining = IsTrainingPath(path);

        var storeReady = false;
        try
        {
            storeReady = await _store.IsAvailableAsync();
        }
        catch
        {
            // treat as no local store
        }

        // Mutations are always applied locally first: the user's action is
        // never blocked by the network and never lost when it fails.
        if (storeReady && !isGet && isTraining)
        {
            var local = await TryLocalAsync(request);
            if (local is not null)
            {
                if (_status.IsOnline)
                {
                    _ = _coordinator.TriggerDrainAsync();
                }

                return local;
            }
        }

        // GETs: the local mirror wins only while there are un-synced local
        // changes for that entity (otherwise the server is the fresher copy).
        if (storeReady && isGet && isTraining && await HasLocalChangesAsync(path))
        {
            var local = await TryLocalAsync(request);
            if (local is not null)
            {
                return local;
            }
        }

        if (storeReady && isGet && !_status.IsOnline)
        {
            var cached = await ServeCachedAsync(request);
            if (cached is not null)
            {
                return cached;
            }

            var local = await TryLocalAsync(request);
            if (local is not null)
            {
                return local;
            }
        }

        // Stale-while-revalidate: when a cached copy exists, the network
        // gets a short budget. A slow network must not make the user wait:
        // answer from the cache now and let the network refresh it later.
        if (storeReady && isGet)
        {
            var cached = await ServeCachedAsync(request);
            if (cached is not null)
            {
                var networkTask = TryNetworkAsync(request, cancellationToken);
                using var budgetCts = new CancellationTokenSource();
                var budgetTask = Task.Delay(SlowNetworkBudget, budgetCts.Token);
                var completed = await Task.WhenAny(networkTask, budgetTask);
                if (completed == networkTask)
                {
                    budgetCts.Cancel();
                    var response = await networkTask;
                    if (response is not null)
                    {
                        return response;
                    }
                }
                else
                {
                    BackgroundRefresh = RefreshCacheInBackgroundAsync(networkTask);
                }

                return cached;
            }
        }

        try
        {
            var response = await SendNetworkAsync(request, cancellationToken);

            if (IsNetworkReachable(response))
            {
                _status.MarkOnline();
            }

            if (isGet && storeReady && response.IsSuccessStatusCode)
            {
                await CacheAsync(request, response);
            }

            return response;
        }
        catch (HttpRequestException)
        {
            _status.MarkOffline();
            if (storeReady && isGet)
            {
                var cached = await ServeCachedAsync(request);
                if (cached is not null)
                {
                    return cached;
                }

                var local = await _mutator.TryHandleAsync(request);
                if (local is not null)
                {
                    return local;
                }
            }
            else if (storeReady && isTraining)
            {
                // The request never left the device: apply it locally so
                // the user's action is not lost.
                var local = await _mutator.TryHandleAsync(request);
                if (local is not null)
                {
                    return local;
                }
            }

            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout: same offline semantics as a network failure.
            _status.MarkOffline();
            if (storeReady && isGet)
            {
                var cached = await ServeCachedAsync(request);
                if (cached is not null)
                {
                    return cached;
                }
            }

            throw;
        }
    }

    private async Task<bool> HasLocalChangesAsync(string path)
    {
        try
        {
            var normalized = path.TrimStart('/').ToLowerInvariant();
            if (normalized.StartsWith("workouts", StringComparison.Ordinal))
            {
                return await _store.HasPendingForEntityAsync(SyncEntities.Workout);
            }

            if (normalized.StartsWith("exercises", StringComparison.Ordinal))
            {
                return await _store.HasPendingForEntityAsync(SyncEntities.Exercise);
            }
        }
        catch
        {
            // fall through to the network path
        }

        return false;
    }

    private static bool IsTrainingPath(string path)
    {
        var normalized = path.TrimStart('/').TrimEnd('/').ToLowerInvariant();
        return MirrorPrefixes.Any(prefix =>
            normalized == prefix || normalized.StartsWith(prefix + "/", StringComparison.Ordinal));
    }

    private static bool IsNetworkReachable(HttpResponseMessage response)
        => (int)response.StatusCode < 500;

    private async Task<HttpResponseMessage?> TryNetworkAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendNetworkAsync(request, cancellationToken);
            if (IsNetworkReachable(response))
            {
                _status.MarkOnline();
            }

            if (response.IsSuccessStatusCode)
            {
                await CacheAsync(request, response);
            }

            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _status.MarkOffline();
            return null;
        }
    }

    private async Task RefreshCacheInBackgroundAsync(Task<HttpResponseMessage?> networkTask)
    {
        try
        {
            // TryNetworkAsync already cached the response and reported
            // failures; this wrapper only keeps the task from going unobserved.
            await networkTask;
        }
        catch
        {
        }
    }

    /// <summary>
    /// The stale-while-revalidate refresh started by the last cache hit served
    /// past the network budget. Exposed so callers that need the refresh (and
    /// therefore its cache write) settled can await it.
    /// </summary>
    public Task BackgroundRefresh { get; private set; } = Task.CompletedTask;

    private async Task<HttpResponseMessage?> ServeCachedAsync(HttpRequestMessage request)
    {
        var key = CacheKey(request);
        var cached = await _store.GetCachedResponseAsync(key);
        if (cached is null)
        {
            return null;
        }

        return new HttpResponseMessage((System.Net.HttpStatusCode)cached.StatusCode)
        {
            Content = new StringContent(cached.Body, System.Text.Encoding.UTF8, cached.ContentType)
        };
    }

    private async Task CacheAsync(HttpRequestMessage request, HttpResponseMessage response)
    {
        var key = CacheKey(request);
        if (!CacheablePrefixes.Any(prefix =>
                key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        string body;
        try
        {
            // Buffered read: cheap, and it keeps the body readable for the caller.
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType
                          ?? "application/json";

        // The write itself (a catalog mirror transaction on the first load) is
        // detached: the cache is for the *next* read, so the page that triggered
        // this one must not wait for it.
        var total = response.Headers.TryGetValues("X-Total-Count", out var totals)
                    && int.TryParse(totals.FirstOrDefault(), out var parsedTotal)
            ? parsedTotal
            : -1;
        LastCacheWrite = WriteCacheAsync(
            key, (int)response.StatusCode, contentType, body, total);
    }

    /// <summary>
    /// The detached cache write started by the last cached response. Exposed so
    /// callers that need the cache settled (tests, diagnostics) can await it
    /// instead of guessing when the background write finished.
    /// </summary>
    public Task LastCacheWrite { get; private set; } = Task.CompletedTask;

    private async Task WriteCacheAsync(
        string key,
        int statusCode,
        string contentType,
        string body,
        int total)
    {
        try
        {
            // Let the awaiting page render its content before the write runs.
            await Task.Yield();

            // The catalog is paged now, so the mirror is the offline source of truth:
            // the page request itself is never replayed from the response cache. Only
            // rows the mirror is still missing get written (the mirror is upserted per
            // row), which keeps the 876-row hydration off the main thread's critical
            // path and makes a warm session write nothing at all.
            if (key.StartsWith("exercisetypes", StringComparison.OrdinalIgnoreCase))
            {
                await MirrorCatalogIfIncompleteAsync("exercise_types", body, "id", total);
                return;
            }

            var hsp = System.Diagnostics.Stopwatch.StartNew();
            await _store.CacheResponseAsync(key, statusCode, contentType, body);
            Console.WriteLine($"HSPERF cachewrite={hsp.ElapsedMilliseconds}ms key={key}");

            // Mirror the read-only catalog so the library works with no cache entry.
            if (key.StartsWith("api/muscles", StringComparison.OrdinalIgnoreCase))
            {
                await MirrorCatalogIfIncompleteAsync("muscles", body, "id", total: -1);
            }
            else if (key.StartsWith("workouts", StringComparison.OrdinalIgnoreCase))
            {
                // Hydrate the entity mirrors from server reads: a workout the
                // server created must exist locally (client_id = server id) or
                // local mutations against it 404 with "Workout not found".
                await IngestWorkoutsAsync(body);
            }
        }
        catch (Exception ex)
        {
            // A cache write can never break the read that returned the data.
            _logger?.LogDebug(ex, "Offline cache write failed for {Key}", key);
        }
    }

    private async Task IngestWorkoutsAsync(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var roots = doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? doc.RootElement.EnumerateArray().ToList()
                : new List<System.Text.Json.JsonElement> { doc.RootElement };

            foreach (var session in roots)
            {
                if (session.ValueKind != System.Text.Json.JsonValueKind.Object
                    || !session.TryGetProperty("id", out var idEl)
                    || !Guid.TryParse(idEl.GetString(), out var sessionId))
                {
                    continue;
                }

                var date = session.TryGetProperty("date", out var dateEl)
                           && DateOnly.TryParse(dateEl.GetString(), out var parsed)
                    ? parsed
                    : DateOnly.FromDateTime(DateTime.UtcNow);
                var type = session.TryGetProperty("type", out var typeEl)
                    ? typeEl.GetString() ?? "Training"
                    : "Training";

                await _store.UpsertWorkoutAsync(new LocalWorkout
                {
                    ClientId = sessionId,
                    ServerId = sessionId,
                    Date = date,
                    Type = type
                });

                if (!session.TryGetProperty("exercises", out var exercises)
                    || exercises.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var ex in exercises.EnumerateArray())
                {
                    if (!ex.TryGetProperty("id", out var exIdEl)
                        || !Guid.TryParse(exIdEl.GetString(), out var exId))
                    {
                        continue;
                    }

                    await _store.UpsertExerciseAsync(new LocalExercise
                    {
                        ClientId = exId,
                        ServerId = exId,
                        WorkoutClientId = sessionId,
                        TypeId = ReadInt(ex, "typeId") ?? 0,
                        Name = ReadString(ex, "name"),
                        Series = ReadInt(ex, "series"),
                        Repetitions = ReadInt(ex, "repetitions"),
                        Weight = ReadDouble(ex, "weight"),
                        BodyWeight = ReadDouble(ex, "bodyWeight"),
                        AddedWeight = ReadDouble(ex, "addedWeight"),
                        Comments = ReadString(ex, "comments"),
                        Tags = ex.TryGetProperty("tags", out var tags)
                               && tags.ValueKind == System.Text.Json.JsonValueKind.Array
                            ? tags.EnumerateArray()
                                .Select(t => t.GetString() ?? string.Empty).ToList()
                            : new List<string>()
                    });
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // not JSON: skip ingestion
        }
    }

    private static string? ReadString(System.Text.Json.JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
            ? v.GetString()
            : null;

    private static int? ReadInt(System.Text.Json.JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
            && v.TryGetInt32(out var n)
            ? n
            : null;

    private static double? ReadDouble(System.Text.Json.JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number
            ? v.GetDouble()
            : null;

    /// <summary>
    /// Mirrors a catalog page only while the mirror is still missing rows. Static
    /// catalogs (muscles) are write-once; the paged exercise catalog is complete
    /// once the mirror holds as many rows as the server reported (X-Total-Count).
    /// Writes are serialised: the pages of a hydration arrive back to back.
    /// </summary>
    private async Task MirrorCatalogIfIncompleteAsync(string kind, string body, string idProperty, int total)
    {
        await _catalogMirrorLock.WaitAsync();
        try
        {
            var mirrored = await _store.CountCatalogAsync(kind);
            if (total > 0 ? mirrored >= total : mirrored > 0)
            {
                return;
            }

            await MirrorCatalogAsync(kind, body, idProperty);
        }
        finally
        {
            _catalogMirrorLock.Release();
        }
    }

    private async Task MirrorCatalogAsync(string kind, string body, string idProperty)
    {
        var hsp2 = System.Diagnostics.Stopwatch.StartNew();
        // Cheap shape check so a non-catalog payload never reaches the store; the
        // per-row work itself happens in the store's worker.
        if (!body.TrimStart().StartsWith('['))
        {
            return;
        }

        try
        {
            await _store.SaveCatalogBodyAsync(kind, body, idProperty);
            Console.WriteLine($"HSPERF mirror-save={hsp2.ElapsedMilliseconds}ms kind={kind}");
        }
        catch (System.Text.Json.JsonException)
        {
            // not JSON: skip mirroring
        }
    }

    private static string CacheKey(HttpRequestMessage request)
    {
        var uri = request.RequestUri;
        var path = (uri?.AbsolutePath ?? "").Trim('/').ToLowerInvariant();
        var query = uri?.Query ?? "";
        return string.IsNullOrEmpty(query) ? path : path + query.ToLowerInvariant();
    }

    /// <summary>
    /// Terminal handler that forwards to the real transport owned by the
    /// caller's HttpClient (a handler instance may only belong to one chain).
    /// </summary>
    internal sealed class PassThroughHandler : DelegatingHandler
    {
        public PassThroughHandler(HttpMessageHandler terminal)
        {
            InnerHandler = terminal;
        }

        // DelegatingHandler.SendAsync is protected; this is the single entry
        // point that lets the owning HttpClient route into it.
        public Task<HttpResponseMessage> Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => SendAsync(request, cancellationToken);
    }
}
