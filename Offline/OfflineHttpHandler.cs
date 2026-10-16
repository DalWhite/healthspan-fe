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
        "workouts", "exercises", "exercisetypes", "api/muscles", "users"
    ];

    private static readonly string[] MirrorPrefixes =
    [
        "workouts", "exercises"
    ];

    private readonly ILocalStore _store;
    private readonly LocalMutator _mutator;
    private readonly SyncStatusService _status;
    private readonly SyncCoordinator _coordinator;
    private HttpClient? _forward;

    public OfflineHttpHandler(
        ILocalStore store,
        LocalMutator mutator,
        SyncStatusService status,
        SyncCoordinator coordinator)
    {
        _store = store;
        _mutator = mutator;
        _status = status;
        _coordinator = coordinator;
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

    private Task<HttpResponseMessage> SendNetworkAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        // ProcessAsync mode: reach the caller's transport through the private
        // forward client. Chain mode (InnerHandler assigned): normal delegation.
        => _forward is not null
            ? _forward.SendAsync(request, cancellationToken)
            : base.SendAsync(request, cancellationToken);

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
            var local = await _mutator.TryHandleAsync(request);
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
            var local = await _mutator.TryHandleAsync(request);
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

            var local = await _mutator.TryHandleAsync(request);
            if (local is not null)
            {
                return local;
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
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType
                          ?? "application/json";
        await _store.CacheResponseAsync(
            key, (int)response.StatusCode, contentType, body);

        // Mirror the read-only catalog so the library works with no cache entry.
        if (key.StartsWith("exercisetypes", StringComparison.OrdinalIgnoreCase))
        {
            await MirrorCatalogAsync("exercise_types", body, "id");
        }
        else if (key.StartsWith("api/muscles", StringComparison.OrdinalIgnoreCase))
        {
            await MirrorCatalogAsync("muscles", body, "id");
        }
    }

    private async Task MirrorCatalogAsync(string kind, string body, string idProperty)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return;
            }

            var items = new List<(string Id, string Payload)>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var id = element.TryGetProperty(idProperty, out var idElement)
                    ? idElement.ToString()
                    : Guid.NewGuid().ToString();
                items.Add((id, element.GetRawText()));
            }

            await _store.SaveCatalogAsync(kind, items);
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
