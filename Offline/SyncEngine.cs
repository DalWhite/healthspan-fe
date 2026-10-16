using System.Net.Http.Json;
using System.Text.Json;

namespace Healthspan.Offline;

/// <summary>
/// Drains the local sync queue against POST /sync in FIFO batches.
/// The server dedupes by OpId, so a retry after a dropped response is safe.
/// </summary>
public sealed class SyncEngine
{
    private const int BatchSize = 50;
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(15);

    private readonly Func<HttpClient> _httpClientFactory;
    private readonly ILocalStore _store;
    private readonly SyncStatusService _status;
    private readonly SemaphoreSlim _drainLock = new(1, 1);
    private CancellationTokenSource? _loopCts;

    public SyncEngine(
        Func<HttpClient> httpClientFactory,
        ILocalStore store,
        SyncStatusService status)
    {
        _httpClientFactory = httpClientFactory;
        _store = store;
        _status = status;
    }

    /// <summary>Starts the idle poll loop (also reacts to Enqueued signals).</summary>
    public void Start(CancellationToken appCancellation)
    {
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(appCancellation);
        _ = RunLoopAsync(_loopCts.Token);
    }

    public void Stop() => _loopCts?.Cancel();

    private async Task RunLoopAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                if (_status.IsOnline && await _store.CountPendingAsync() > 0)
                {
                    await DrainAsync(cancellation);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[sync] background drain failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(IdlePollInterval, cancellation);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Attempts to flush the queue. Returns the number of operations applied
    /// (or deduped) by the server. Safe to call concurrently: only one drain
    /// runs at a time.
    /// </summary>
    public async Task<int> DrainAsync(CancellationToken cancellation = default)
    {
        if (!await _drainLock.WaitAsync(0, cancellation))
        {
            return 0;
        }

        try
        {
            var totalApplied = 0;
            while (true)
            {
                var batch = await _store.PeekQueueAsync(BatchSize);
                if (batch.Count == 0)
                {
                    break;
                }

                _status.SetSyncing(true);
                try
                {
                    var client = _httpClientFactory();
                    var request = BuildRequest(batch);
                    using var response = await client.PostAsJsonAsync(
                        "/sync", request, cancellation);

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Not a connectivity problem: keep the queue intact.
                        return totalApplied;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        await ReportFailureAsync(batch, $"HTTP {(int)response.StatusCode}");
                        if ((int)response.StatusCode >= 500)
                        {
                            _status.MarkOffline();
                        }

                        return totalApplied;
                    }

                    _status.MarkOnline();
                    var result = await response.Content
                        .ReadFromJsonAsync<SyncResponse>() ?? new SyncResponse();

                    await ApplyResultAsync(batch, result);
                    totalApplied += result.Applied + result.Deduped;

                    // A partial batch means the rest depends on ops that failed;
                    // stop and let the user see the problem instead of looping.
                    if (result.Failed.Count > 0)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Network-level failure: the server may or may not have
                    // applied the batch; OpId dedupe makes the retry safe.
                    _status.MarkOffline();
                    await ReportFailureAsync(batch, ex.Message);
                    return totalApplied;
                }
                finally
                {
                    _status.SetSyncing(false);
                }
            }

            return totalApplied;
        }
        finally
        {
            await RefreshPendingCountAsync();
            _drainLock.Release();
        }
    }

    private static SyncRequest BuildRequest(List<SyncOperation> batch) => new()
    {
        Operations = batch.Select(op => new SyncOperationPayload
        {
            OpId = op.OpId,
            Entity = op.Entity,
            Action = op.Action,
            ClientEntityId = op.ClientEntityId,
            Payload = ParsePayload(op.Payload)
        }).ToList()
    };

    private static JsonElement ParsePayload(string payload)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload)
                        .RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}").RootElement;
        }
    }

    private async Task ApplyResultAsync(List<SyncOperation> batch, SyncResponse result)
    {
        foreach (var mapping in result.IdMap)
        {
            await _store.RemapEntityIdAsync(mapping.ClientId, mapping.ServerId);
        }

        // Server-rejected ops are dropped too: replaying the same op would
        // fail forever, and the user is told about them via the failure log.
        foreach (var failure in result.Failed)
        {
            await _store.MarkAttemptAsync(failure.OpId, failure.Reason);
        }

        var completedIds = batch.Select(op => op.OpId).ToList();
        if (completedIds.Count > 0)
        {
            await _store.RemoveFromQueueAsync(completedIds);
        }
    }

    private async Task ReportFailureAsync(
        List<SyncOperation> batch, string error)
    {
        foreach (var op in batch)
        {
            await _store.MarkAttemptAsync(op.OpId, error);
        }
    }

    public async Task RefreshPendingCountAsync()
    {
        var pending = 0;
        try
        {
            pending = await _store.CountPendingAsync();
        }
        catch
        {
            // keep last known value
        }

        _status.SetPendingCount(pending);
    }
}
