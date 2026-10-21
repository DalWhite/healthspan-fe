namespace Healthspan.Offline;

/// <summary>
/// Abstraction over the local SQLite store so the offline/sync layer can be
/// tested without a WASM runtime (the tests use the in-memory implementation).
/// All SQL lives inside the store implementations.
/// </summary>
public interface ILocalStore
{
    Task<bool> IsAvailableAsync();

    // ── Sync queue ────────────────────────────────────────────────────────────
    Task EnqueueAsync(SyncOperation operation);
    Task<List<SyncOperation>> PeekQueueAsync(int limit);
    Task RemoveFromQueueAsync(IEnumerable<Guid> opIds);
    Task MarkAttemptAsync(Guid opId, string? error);
    Task<int> CountPendingAsync();
    Task<bool> HasPendingForEntityAsync(string entity);

    // ── GET response cache ────────────────────────────────────────────────────
    Task CacheResponseAsync(string cacheKey, int statusCode, string contentType, string body);
    Task<LocalCachedResponse?> GetCachedResponseAsync(string cacheKey);
    Task InvalidateCacheAsync(string prefix);

    // ── Read-only catalog mirror (exercise library, muscles) ──────────────────
    Task SaveCatalogAsync(string kind, IEnumerable<(string Id, string Payload)> items);
    Task<List<string>> GetCatalogPayloadsAsync(string kind);
    Task<bool> HasCatalogAsync(string kind);
    Task InvalidateCatalogAsync(string kind);

    // ── Entity mirrors (workouts / exercises) ─────────────────────────────────
    Task UpsertWorkoutAsync(LocalWorkout workout);
    Task<LocalWorkout?> GetWorkoutAsync(Guid clientId);
    Task<List<LocalWorkout>> GetWorkoutsAsync(
        bool includeDeleted = false,
        DateOnly? date = null,
        int? limit = null,
        int? offset = null);
    Task MarkWorkoutDeletedAsync(Guid clientId);

    Task UpsertExerciseAsync(LocalExercise exercise);
    Task<LocalExercise?> GetExerciseAsync(Guid clientId);
    Task<List<LocalExercise>> GetExercisesForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false);
    Task MarkExerciseDeletedAsync(Guid clientId);
    Task MarkWorkoutExercisesDeletedAsync(Guid workoutClientId);

    Task UpsertBlockAsync(LocalBlock block);
    Task<LocalBlock?> GetBlockAsync(Guid clientId);
    Task<List<LocalBlock>> GetBlocksForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false);
    Task MarkBlockDeletedAsync(Guid clientId);
    Task MarkWorkoutBlocksDeletedAsync(Guid workoutClientId);

    /// <summary>
    /// After a successful sync, points the client-generated id at the
    /// server-generated id so later ops reference the canonical entity.
    /// </summary>
    Task RemapEntityIdAsync(Guid clientEntityId, Guid serverEntityId);
}

public readonly record struct LocalStoreChanges(int Changes, long LastInsertRowId);

public sealed record LocalCachedResponse(int StatusCode, string ContentType, string Body);

public sealed class LocalWorkout
{
    public Guid ClientId { get; init; }
    public Guid? ServerId { get; set; }
    public DateOnly Date { get; set; }
    public string Type { get; set; } = "Training";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }

    // Universal tracker. A null PerceivedFatigue means the session is still
    // open; the score itself is what closes it (the API stamps CompletedAt).
    public string? Goal { get; set; }
    public List<string> SecondaryGoals { get; set; } = [];
    public string? Modality { get; set; }
    public int? PerceivedFatigue { get; set; }
}

public sealed class LocalExercise
{
    public Guid ClientId { get; init; }
    public Guid? ServerId { get; set; }
    public Guid WorkoutClientId { get; init; }
    public int TypeId { get; set; }
    public string? Name { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public double? Weight { get; set; }
    public double? BodyWeight { get; set; }
    public double? AddedWeight { get; set; }
    public string? Comments { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }

    // Block membership is by CLIENT id: while offline the server id of a block
    // does not exist yet, and the sync remap resolves it later.
    public Guid? BlockClientId { get; set; }
    public int Order { get; set; }
    public int Attempts { get; set; } = 1;
    public int CompletedAttempts { get; set; } = 1;
    public string? QualityIntent { get; set; }
    public string? LoadType { get; set; }

    public double Volume =>
        (Series.GetValueOrDefault() * Repetitions.GetValueOrDefault())
        * (Weight ?? BodyWeight ?? AddedWeight ?? 0);
}

public sealed class LocalBlock
{
    public Guid ClientId { get; init; }
    public Guid? ServerId { get; set; }
    public Guid WorkoutClientId { get; init; }
    public int Order { get; set; }
    public string Type { get; set; } = "Normal";
    public string? Label { get; set; }
    public int? Rounds { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; }
}
