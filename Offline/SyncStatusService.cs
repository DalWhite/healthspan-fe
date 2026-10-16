namespace Healthspan.Offline;

public enum SyncUiStatus
{
    Synced,
    Offline,
    Pending,
    Syncing
}

/// <summary>
/// Single source of truth for the offline/sync indicator. Connectivity is
/// inferred from real request outcomes (the DelegatingHandler reports them)
/// and from the browser online/offline events, so it never lies to the user.
/// </summary>
public sealed class SyncStatusService
{
    private readonly object _gate = new();
    private bool _isOnline = true;
    private int _pendingCount;
    private bool _isSyncing;

    public event Action? StatusChanged;

    public bool IsOnline
    {
        get { lock (_gate) return _isOnline; }
    }

    public int PendingCount
    {
        get { lock (_gate) return _pendingCount; }
    }

    public bool IsSyncing
    {
        get { lock (_gate) return _isSyncing; }
    }

    public SyncUiStatus CurrentStatus
    {
        get
        {
            lock (_gate)
            {
                if (_isSyncing)
                {
                    return SyncUiStatus.Syncing;
                }

                if (!_isOnline)
                {
                    return SyncUiStatus.Offline;
                }

                return _pendingCount > 0 ? SyncUiStatus.Pending : SyncUiStatus.Synced;
            }
        }
    }

    public void MarkOnline() => Update(() => _isOnline = true);

    public void MarkOffline() => Update(() => _isOnline = false);

    public void SetPendingCount(int count) => Update(() => _pendingCount = count);

    public void SetSyncing(bool syncing) => Update(() => _isSyncing = syncing);

    private void Update(Action mutate)
    {
        lock (_gate)
        {
            mutate();
        }

        StatusChanged?.Invoke();
    }
}
