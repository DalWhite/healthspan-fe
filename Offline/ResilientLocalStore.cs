namespace Healthspan.Offline;

/// <summary>
/// Tries the SQLite-backed store first and transparently falls back to the
/// in-memory store if the WASM database cannot be opened in this browser.
/// </summary>
public sealed class ResilientLocalStore : ILocalStore
{
    private readonly JsLocalStore _primary;
    private readonly InMemoryLocalStore _fallback;
    private bool _primaryBroken;

    public ResilientLocalStore(JsLocalStore primary, InMemoryLocalStore fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public InMemoryLocalStore Fallback => _fallback;

    private async Task<ILocalStore> PickAsync()
    {
        if (!_primaryBroken && await _primary.IsAvailableAsync())
        {
            return _primary;
        }

        _primaryBroken = true;
        return _fallback;
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    private async Task<T> RunAsync<T>(Func<ILocalStore, Task<T>> action)
        => await action(await PickAsync());

    private async Task RunAsync(Func<ILocalStore, Task> action)
        => await action(await PickAsync());

    public Task EnqueueAsync(SyncOperation operation)
        => RunAsync(store => store.EnqueueAsync(operation));

    public Task<List<SyncOperation>> PeekQueueAsync(int limit)
        => RunAsync(store => store.PeekQueueAsync(limit));

    public Task RemoveFromQueueAsync(IEnumerable<Guid> opIds)
        => RunAsync(store => store.RemoveFromQueueAsync(opIds));

    public Task MarkAttemptAsync(Guid opId, string? error)
        => RunAsync(store => store.MarkAttemptAsync(opId, error));

    public Task<int> CountPendingAsync()
        => RunAsync(store => store.CountPendingAsync());

    public Task<bool> HasPendingForEntityAsync(string entity)
        => RunAsync(store => store.HasPendingForEntityAsync(entity));

    public Task CacheResponseAsync(string cacheKey, int statusCode, string contentType, string body)
        => RunAsync(store => store.CacheResponseAsync(cacheKey, statusCode, contentType, body));

    public Task<LocalCachedResponse?> GetCachedResponseAsync(string cacheKey)
        => RunAsync(store => store.GetCachedResponseAsync(cacheKey));

    public Task InvalidateCacheAsync(string prefix)
        => RunAsync(store => store.InvalidateCacheAsync(prefix));

    public Task SaveCatalogAsync(string kind, IEnumerable<(string Id, string Payload)> items)
        => RunAsync(store => store.SaveCatalogAsync(kind, items));

    public Task SaveCatalogBodyAsync(string kind, string body, string idProperty)
        => RunAsync(store => store.SaveCatalogBodyAsync(kind, body, idProperty));

    public Task<List<string>> GetCatalogPayloadsAsync(string kind)
        => RunAsync(store => store.GetCatalogPayloadsAsync(kind));

    public Task<(List<string> Payloads, int Total)> GetCatalogPayloadsPageAsync(
        string kind, int skip, int take)
        => RunAsync(store => store.GetCatalogPayloadsPageAsync(kind, skip, take));

    public Task<bool> HasCatalogAsync(string kind)
        => RunAsync(store => store.HasCatalogAsync(kind));

    public Task<int> CountCatalogAsync(string kind)
        => RunAsync(store => store.CountCatalogAsync(kind));

    public Task InvalidateCatalogAsync(string kind)
        => RunAsync(store => store.InvalidateCatalogAsync(kind));

    public Task UpsertWorkoutAsync(LocalWorkout workout)
        => RunAsync(store => store.UpsertWorkoutAsync(workout));

    public Task<LocalWorkout?> GetWorkoutAsync(Guid clientId)
        => RunAsync(store => store.GetWorkoutAsync(clientId));

    public Task<List<LocalWorkout>> GetWorkoutsAsync(
        bool includeDeleted = false,
        DateOnly? date = null,
        int? limit = null,
        int? offset = null)
        => RunAsync(store => store.GetWorkoutsAsync(includeDeleted, date, limit, offset));

    public Task MarkWorkoutDeletedAsync(Guid clientId)
        => RunAsync(store => store.MarkWorkoutDeletedAsync(clientId));

    public Task UpsertExerciseAsync(LocalExercise exercise)
        => RunAsync(store => store.UpsertExerciseAsync(exercise));

    public Task<LocalExercise?> GetExerciseAsync(Guid clientId)
        => RunAsync(store => store.GetExerciseAsync(clientId));

    public Task<List<LocalExercise>> GetExercisesForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
        => RunAsync(store => store.GetExercisesForWorkoutAsync(workoutClientId, includeDeleted));

    public Task MarkExerciseDeletedAsync(Guid clientId)
        => RunAsync(store => store.MarkExerciseDeletedAsync(clientId));

    public Task MarkWorkoutExercisesDeletedAsync(Guid workoutClientId)
        => RunAsync(store => store.MarkWorkoutExercisesDeletedAsync(workoutClientId));

    public Task UpsertBlockAsync(LocalBlock block)
        => RunAsync(store => store.UpsertBlockAsync(block));

    public Task<LocalBlock?> GetBlockAsync(Guid clientId)
        => RunAsync(store => store.GetBlockAsync(clientId));

    public Task<List<LocalBlock>> GetBlocksForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
        => RunAsync(store => store.GetBlocksForWorkoutAsync(workoutClientId, includeDeleted));

    public Task MarkBlockDeletedAsync(Guid clientId)
        => RunAsync(store => store.MarkBlockDeletedAsync(clientId));

    public Task MarkWorkoutBlocksDeletedAsync(Guid workoutClientId)
        => RunAsync(store => store.MarkWorkoutBlocksDeletedAsync(workoutClientId));

    public Task RemapEntityIdAsync(Guid clientEntityId, Guid serverEntityId)
        => RunAsync(store => store.RemapEntityIdAsync(clientEntityId, serverEntityId));
}
