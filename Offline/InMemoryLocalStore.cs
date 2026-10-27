namespace Healthspan.Offline;

/// <summary>
/// In-memory implementation of <see cref="ILocalStore"/> used by bUnit tests
/// (and as a last-resort fallback in the browser when SQLite WASM or OPFS
/// cannot be initialised). Semantics mirror the SQLite tables.
/// </summary>
public sealed class InMemoryLocalStore : ILocalStore
{
    private readonly Dictionary<Guid, SyncOperation> _queue = [];
    private readonly Dictionary<string, LocalCachedResponse> _responses = [];
    private readonly Dictionary<string, Dictionary<string, string>> _catalog = [];
    private readonly Dictionary<Guid, LocalWorkout> _workouts = [];
    private readonly Dictionary<Guid, LocalExercise> _exercises = [];
    private readonly Dictionary<Guid, LocalBlock> _blocks = [];

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    // ── Sync queue ────────────────────────────────────────────────────────────

    public Task EnqueueAsync(SyncOperation operation)
    {
        _queue[operation.OpId] = operation;
        return Task.CompletedTask;
    }

    public Task<List<SyncOperation>> PeekQueueAsync(int limit)
        => Task.FromResult(
            _queue.Values
                .OrderBy(op => op.CreatedAtUtc)
                .ThenBy(op => op.OpId)
                .Take(limit)
                .ToList());

    public Task RemoveFromQueueAsync(IEnumerable<Guid> opIds)
    {
        foreach (var opId in opIds)
        {
            _queue.Remove(opId);
        }

        return Task.CompletedTask;
    }

    public Task MarkAttemptAsync(Guid opId, string? error)
    {
        if (_queue.TryGetValue(opId, out var op))
        {
            op.Attempts++;
            op.LastError = error;
        }

        return Task.CompletedTask;
    }

    public Task<int> CountPendingAsync() => Task.FromResult(_queue.Count);

    public Task<bool> HasPendingForEntityAsync(string entity)
        => Task.FromResult(_queue.Values.Any(op => op.Entity == entity));

    // ── GET response cache ────────────────────────────────────────────────────

    public Task CacheResponseAsync(
        string cacheKey, int statusCode, string contentType, string body)
    {
        _responses[cacheKey] = new LocalCachedResponse(statusCode, contentType, body);
        return Task.CompletedTask;
    }

    public Task<LocalCachedResponse?> GetCachedResponseAsync(string cacheKey)
        => Task.FromResult<LocalCachedResponse?>(
            _responses.TryGetValue(cacheKey, out var cached) ? cached : null);

    public Task InvalidateCacheAsync(string prefix)
    {
        foreach (var key in _responses.Keys
                     .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                     .ToList())
        {
            _responses.Remove(key);
        }

        return Task.CompletedTask;
    }

    // ── Catalog mirror ────────────────────────────────────────────────────────

    public Task SaveCatalogAsync(
        string kind, IEnumerable<(string Id, string Payload)> items)
    {
        if (!_catalog.TryGetValue(kind, out var bucket))
        {
            bucket = [];
            _catalog[kind] = bucket;
        }

        foreach (var (id, payload) in items)
        {
            bucket[id] = payload;
        }

        return Task.CompletedTask;
    }

    public Task<List<string>> GetCatalogPayloadsAsync(string kind)
        => Task.FromResult(
            _catalog.TryGetValue(kind, out var bucket)
                ? bucket.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList()
                : []);

    public Task<(List<string> Payloads, int Total)> GetCatalogPayloadsPageAsync(
        string kind, int skip, int take)
    {
        if (!_catalog.TryGetValue(kind, out var bucket))
        {
            return Task.FromResult<(List<string>, int)>(([], 0));
        }

        var ordered = bucket.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value);
        var page = take <= 0
            ? ordered.Skip(Math.Max(0, skip))
            : ordered.Skip(Math.Max(0, skip)).Take(take);
        return Task.FromResult((page.ToList(), bucket.Count));
    }

    public Task<bool> HasCatalogAsync(string kind)
        => Task.FromResult(_catalog.TryGetValue(kind, out var bucket) && bucket.Count > 0);

    public Task<int> CountCatalogAsync(string kind)
        => Task.FromResult(_catalog.TryGetValue(kind, out var bucket) ? bucket.Count : 0);

    public Task InvalidateCatalogAsync(string kind)
    {
        _catalog.Remove(kind);
        return Task.CompletedTask;
    }

    // ── Entity mirrors ────────────────────────────────────────────────────────

    public Task UpsertWorkoutAsync(LocalWorkout workout)
    {
        _workouts[workout.ClientId] = workout;
        return Task.CompletedTask;
    }

    public Task<LocalWorkout?> GetWorkoutAsync(Guid clientId)
        => Task.FromResult<LocalWorkout?>(
            _workouts.TryGetValue(clientId, out var workout) ? workout : null);

    public Task<List<LocalWorkout>> GetWorkoutsAsync(
        bool includeDeleted = false,
        DateOnly? date = null,
        int? limit = null,
        int? offset = null)
    {
        IEnumerable<LocalWorkout> query = _workouts.Values;
        if (!includeDeleted)
        {
            query = query.Where(w => !w.Deleted);
        }

        if (date.HasValue)
        {
            query = query.Where(w => w.Date == date.Value);
        }

        query = query
            .OrderByDescending(w => w.Date)
            .ThenBy(w => w.ClientId);

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return Task.FromResult(query.ToList());
    }

    public Task MarkWorkoutDeletedAsync(Guid clientId)
    {
        if (_workouts.TryGetValue(clientId, out var workout))
        {
            workout.Deleted = true;
            workout.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task UpsertExerciseAsync(LocalExercise exercise)
    {
        _exercises[exercise.ClientId] = exercise;
        return Task.CompletedTask;
    }

    public Task<LocalExercise?> GetExerciseAsync(Guid clientId)
        => Task.FromResult<LocalExercise?>(
            _exercises.TryGetValue(clientId, out var exercise) ? exercise : null);

    public Task<List<LocalExercise>> GetExercisesForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
        => Task.FromResult(
            _exercises.Values
                .Where(e => e.WorkoutClientId == workoutClientId
                            && (includeDeleted || !e.Deleted))
                .OrderBy(e => e.UpdatedAtUtc)
                .ToList());

    public Task MarkExerciseDeletedAsync(Guid clientId)
    {
        if (_exercises.TryGetValue(clientId, out var exercise))
        {
            exercise.Deleted = true;
            exercise.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task MarkWorkoutExercisesDeletedAsync(Guid workoutClientId)
    {
        foreach (var exercise in _exercises.Values
                     .Where(e => e.WorkoutClientId == workoutClientId))
        {
            exercise.Deleted = true;
            exercise.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task UpsertBlockAsync(LocalBlock block)
    {
        _blocks[block.ClientId] = block;
        return Task.CompletedTask;
    }

    public Task<LocalBlock?> GetBlockAsync(Guid clientId)
        => Task.FromResult<LocalBlock?>(
            _blocks.TryGetValue(clientId, out var block) ? block : null);

    public Task<List<LocalBlock>> GetBlocksForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
        => Task.FromResult(
            _blocks.Values
                .Where(b => b.WorkoutClientId == workoutClientId
                            && (includeDeleted || !b.Deleted))
                .OrderBy(b => b.Order)
                .ThenBy(b => b.UpdatedAtUtc)
                .ToList());

    public Task MarkBlockDeletedAsync(Guid clientId)
    {
        if (_blocks.TryGetValue(clientId, out var block))
        {
            block.Deleted = true;
            block.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task MarkWorkoutBlocksDeletedAsync(Guid workoutClientId)
    {
        foreach (var block in _blocks.Values
                     .Where(b => b.WorkoutClientId == workoutClientId))
        {
            block.Deleted = true;
            block.UpdatedAtUtc = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task RemapEntityIdAsync(Guid clientEntityId, Guid serverEntityId)
    {
        if (_workouts.TryGetValue(clientEntityId, out var workout))
        {
            workout.ServerId = serverEntityId;
        }

        if (_exercises.TryGetValue(clientEntityId, out var exercise))
        {
            exercise.ServerId = serverEntityId;
        }

        if (_blocks.TryGetValue(clientEntityId, out var block))
        {
            block.ServerId = serverEntityId;
        }

        return Task.CompletedTask;
    }

    // Test helpers -------------------------------------------------------------

    public IReadOnlyDictionary<Guid, SyncOperation> Queue => _queue;
    public IReadOnlyDictionary<Guid, LocalWorkout> Workouts => _workouts;
    public IReadOnlyDictionary<Guid, LocalExercise> Exercises => _exercises;
    public IReadOnlyDictionary<Guid, LocalBlock> Blocks => _blocks;
}
