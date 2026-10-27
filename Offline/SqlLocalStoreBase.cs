using System.Text.Json;

namespace Healthspan.Offline;

/// <summary>
/// SQL implementation of <see cref="ILocalStore"/> shared by the SQLite-WASM
/// backend and the in-memory test backend (which only has to honour the
/// exact statements this class issues).
/// </summary>
public abstract class SqlLocalStoreBase : ILocalStore
{
    public abstract Task<bool> IsAvailableAsync();

    protected abstract Task<List<Dictionary<string, string?>>> QueryAsync(
        string sql, params object?[] args);

    protected abstract Task<LocalStoreChanges> ExecuteAsync(
        string sql, params object?[] args);

    /// <summary>
    /// Applies many statements in one round trip. The default loops
    /// ExecuteAsync; backends with a real transaction API override it.
    /// </summary>
    protected virtual async Task ExecuteBatchAsync(
        IEnumerable<(string Sql, object?[] Args)> statements)
    {
        foreach (var (sql, args) in statements)
        {
            await ExecuteAsync(sql, args);
        }
    }

    // ── Sync queue ────────────────────────────────────────────────────────────

    public async Task EnqueueAsync(SyncOperation operation)
    {
        await ExecuteAsync(
            """
            INSERT OR REPLACE INTO sync_queue
                (op_id, entity, action, client_entity_id, payload, created_at_utc, attempts)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            """,
            operation.OpId.ToString(), operation.Entity, operation.Action,
            operation.ClientEntityId?.ToString(), operation.Payload,
            operation.CreatedAtUtc.ToString("O"), operation.Attempts);
    }

    public async Task<List<SyncOperation>> PeekQueueAsync(int limit)
    {
        var rows = await QueryAsync(
            """
            SELECT op_id, entity, action, client_entity_id, payload,
                   created_at_utc, attempts, last_error
            FROM sync_queue
            ORDER BY created_at_utc ASC
            LIMIT ?
            """,
            limit);
        return rows.Select(MapOperation).ToList();
    }

    protected static SyncOperation MapOperation(Dictionary<string, string?> row)
        => new()
        {
            OpId = Guid.Parse(row["op_id"]!),
            Entity = row["entity"]!,
            Action = row["action"]!,
            ClientEntityId = Guid.TryParse(row["client_entity_id"], out var id) ? id : null,
            Payload = row["payload"] ?? "{}",
            CreatedAtUtc = DateTime.TryParse(
                row["created_at_utc"], null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var created) ? created : DateTime.UtcNow,
            Attempts = int.TryParse(row["attempts"], out var attempts) ? attempts : 0,
            LastError = row["last_error"]
        };

    public async Task RemoveFromQueueAsync(IEnumerable<Guid> opIds)
    {
        foreach (var opId in opIds)
        {
            await ExecuteAsync("DELETE FROM sync_queue WHERE op_id = ?", opId.ToString());
        }
    }

    public async Task MarkAttemptAsync(Guid opId, string? error)
    {
        await ExecuteAsync(
            "UPDATE sync_queue SET attempts = attempts + 1, last_error = ? WHERE op_id = ?",
            error, opId.ToString());
    }

    public async Task<int> CountPendingAsync()
    {
        var rows = await QueryAsync("SELECT COUNT(*) AS n FROM sync_queue");
        return rows.Count > 0 && int.TryParse(rows[0]["n"], out var n) ? n : 0;
    }

    public async Task<bool> HasPendingForEntityAsync(string entity)
    {
        var rows = await QueryAsync(
            "SELECT COUNT(*) AS n FROM sync_queue WHERE entity = ?", entity);
        return rows.Count > 0 && int.TryParse(rows[0]["n"], out var n) && n > 0;
    }

    // ── GET response cache ────────────────────────────────────────────────────

    public async Task CacheResponseAsync(
        string cacheKey, int statusCode, string contentType, string body)
    {
        await ExecuteAsync(
            """
            INSERT OR REPLACE INTO response_cache
                (cache_key, status_code, content_type, body, cached_at_utc)
            VALUES (?, ?, ?, ?, ?)
            """,
            cacheKey, statusCode, contentType, body, DateTime.UtcNow.ToString("O"));
    }

    public async Task<LocalCachedResponse?> GetCachedResponseAsync(string cacheKey)
    {
        var rows = await QueryAsync(
            "SELECT status_code, content_type, body FROM response_cache WHERE cache_key = ?",
            cacheKey);
        if (rows.Count == 0)
        {
            return null;
        }

        return new LocalCachedResponse(
            int.Parse(rows[0]["status_code"]!),
            rows[0]["content_type"] ?? "application/json",
            rows[0]["body"] ?? "");
    }

    public async Task InvalidateCacheAsync(string prefix)
    {
        await ExecuteAsync("DELETE FROM response_cache WHERE cache_key LIKE ?", prefix + "%");
    }

    // ── Catalog mirror ────────────────────────────────────────────────────────

    public async Task SaveCatalogAsync(
        string kind, IEnumerable<(string Id, string Payload)> items)
    {
        var cachedAt = DateTime.UtcNow.ToString("O");
        await ExecuteBatchAsync(
            items.Select(item =>
                ("INSERT OR REPLACE INTO mirror_catalog (kind, item_id, payload, cached_at_utc) VALUES (?, ?, ?, ?)",
                 new object?[] { kind, item.Id, item.Payload, cachedAt })));
    }

    /// <summary>
    /// Mirrors a whole catalog response body. The base implementation splits the
    /// body on the main thread; the WASM store overrides this so the split runs
    /// inside the SQLite worker instead (t_81a400b3).
    /// </summary>
    public virtual Task SaveCatalogBodyAsync(string kind, string body, string idProperty)
        => SaveCatalogAsync(kind, CatalogMirrorBody.Split(body, idProperty));

    public async Task<List<string>> GetCatalogPayloadsAsync(string kind)
    {
        var rows = await QueryAsync(
            "SELECT payload FROM mirror_catalog WHERE kind = ? ORDER BY item_id",
            kind);
        return rows.Select(row => row["payload"] ?? "{}").ToList();
    }

    public async Task<(List<string> Payloads, int Total)> GetCatalogPayloadsPageAsync(
        string kind, int skip, int take)
    {
        var total = await CountCatalogAsync(kind);
        var rows = await QueryAsync(
            "SELECT payload FROM mirror_catalog WHERE kind = ? ORDER BY item_id LIMIT ? OFFSET ?",
            kind,
            take <= 0 ? -1 : take,
            Math.Max(0, skip));
        return (rows.Select(row => row["payload"] ?? "{}").ToList(), total);
    }

    public async Task<int> CountCatalogAsync(string kind)
    {
        var rows = await QueryAsync(
            "SELECT COUNT(*) AS n FROM mirror_catalog WHERE kind = ?",
            kind);
        return rows.Count > 0 && int.TryParse(rows[0]["n"], out var n) ? n : 0;
    }

    public async Task<bool> HasCatalogAsync(string kind)
    {
        var rows = await QueryAsync(
            "SELECT COUNT(*) AS n FROM mirror_catalog WHERE kind = ?",
            kind);
        return rows.Count > 0 && int.TryParse(rows[0]["n"], out var n) && n > 0;
    }

    public async Task InvalidateCatalogAsync(string kind)
    {
        await ExecuteAsync("DELETE FROM mirror_catalog WHERE kind = ?", kind);
    }

    // ── Entity mirrors ────────────────────────────────────────────────────────

    public async Task UpsertWorkoutAsync(LocalWorkout workout)
    {
        await ExecuteAsync(
            """
            INSERT OR REPLACE INTO workouts
                (client_id, server_id, date, type, updated_at_utc, deleted,
                 goal, secondary_goals, modality, perceived_fatigue)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            workout.ClientId.ToString(), workout.ServerId?.ToString(),
            workout.Date.ToString("yyyy-MM-dd"), workout.Type,
            workout.UpdatedAtUtc.ToString("O"), workout.Deleted ? 1 : 0,
            workout.Goal, JsonSerializer.Serialize(workout.SecondaryGoals),
            workout.Modality, workout.PerceivedFatigue);
    }

    public async Task<LocalWorkout?> GetWorkoutAsync(Guid clientId)
    {
        var rows = await QueryAsync(
            "SELECT * FROM workouts WHERE client_id = ?", clientId.ToString());
        return rows.Count > 0 ? MapWorkout(rows[0]) : null;
    }

    public async Task<List<LocalWorkout>> GetWorkoutsAsync(
        bool includeDeleted = false,
        DateOnly? date = null,
        int? limit = null,
        int? offset = null)
    {
        var sql = "SELECT * FROM workouts";
        var conditions = new List<string>();
        var args = new List<object?>();

        if (!includeDeleted)
        {
            conditions.Add("deleted = 0");
        }

        if (date.HasValue)
        {
            conditions.Add("date = ?");
            args.Add(date.Value.ToString("yyyy-MM-dd"));
        }

        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        sql += " ORDER BY date DESC, client_id ASC";

        if (limit.HasValue)
        {
            sql += " LIMIT ?";
            args.Add(limit.Value);
        }

        if (offset.HasValue)
        {
            sql += " OFFSET ?";
            args.Add(offset.Value);
        }

        var rows = await QueryAsync(sql, args.ToArray());
        return rows.Select(MapWorkout).ToList();
    }

    protected static LocalWorkout MapWorkout(Dictionary<string, string?> row)
        => new()
        {
            ClientId = Guid.Parse(row["client_id"]!),
            ServerId = Guid.TryParse(row["server_id"], out var serverId) ? serverId : null,
            Date = DateOnly.TryParse(row["date"], out var date) ? date : DateOnly.FromDateTime(DateTime.UtcNow),
            Type = row["type"] ?? "Training",
            UpdatedAtUtc = DateTime.TryParse(
                row["updated_at_utc"], null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var updated) ? updated : DateTime.UtcNow,
            Deleted = row["deleted"] == "1",
            Goal = Column(row, "goal"),
            SecondaryGoals = ParseStringList(Column(row, "secondary_goals")),
            Modality = Column(row, "modality"),
            PerceivedFatigue = int.TryParse(Column(row, "perceived_fatigue"), out var fatigue)
                ? fatigue
                : null
        };

    public async Task MarkWorkoutDeletedAsync(Guid clientId)
    {
        await ExecuteAsync(
            "UPDATE workouts SET deleted = 1, updated_at_utc = ? WHERE client_id = ?",
            DateTime.UtcNow.ToString("O"), clientId.ToString());
    }

    public async Task UpsertExerciseAsync(LocalExercise exercise)
    {
        await ExecuteAsync(
            """
            INSERT OR REPLACE INTO exercises
                (client_id, server_id, workout_client_id, type_id, name,
                 series, repetitions, weight, body_weight, added_weight,
                 comments, tags, updated_at_utc, deleted,
                 block_client_id, sort_order, attempts, completed_attempts,
                 quality_intent, load_type)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            exercise.ClientId.ToString(), exercise.ServerId?.ToString(),
            exercise.WorkoutClientId.ToString(), exercise.TypeId, exercise.Name,
            exercise.Series, exercise.Repetitions, exercise.Weight,
            exercise.BodyWeight, exercise.AddedWeight, exercise.Comments,
            JsonSerializer.Serialize(exercise.Tags),
            exercise.UpdatedAtUtc.ToString("O"), exercise.Deleted ? 1 : 0,
            exercise.BlockClientId?.ToString(), exercise.Order, exercise.Attempts,
            exercise.CompletedAttempts, exercise.QualityIntent, exercise.LoadType);
    }

    public async Task<LocalExercise?> GetExerciseAsync(Guid clientId)
    {
        var rows = await QueryAsync(
            "SELECT * FROM exercises WHERE client_id = ?", clientId.ToString());
        return rows.Count > 0 ? MapExercise(rows[0]) : null;
    }

    public async Task<List<LocalExercise>> GetExercisesForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
    {
        var sql = "SELECT * FROM exercises WHERE workout_client_id = ?";
        if (!includeDeleted)
        {
            sql += " AND deleted = 0";
        }

        sql += " ORDER BY sort_order ASC, updated_at_utc ASC";

        var rows = await QueryAsync(sql, workoutClientId.ToString());
        return rows.Select(MapExercise).ToList();
    }

    protected static LocalExercise MapExercise(Dictionary<string, string?> row)
        => new()
        {
            ClientId = Guid.Parse(row["client_id"]!),
            ServerId = Guid.TryParse(row["server_id"], out var serverId) ? serverId : null,
            WorkoutClientId = Guid.Parse(row["workout_client_id"]!),
            TypeId = int.TryParse(row["type_id"], out var typeId) ? typeId : 0,
            Name = row["name"],
            Series = int.TryParse(row["series"], out var series) ? series : null,
            Repetitions = int.TryParse(row["repetitions"], out var reps) ? reps : null,
            Weight = ParseDouble(row["weight"]),
            BodyWeight = ParseDouble(row["body_weight"]),
            AddedWeight = ParseDouble(row["added_weight"]),
            Comments = row["comments"],
            Tags = ParseTags(row["tags"]),
            UpdatedAtUtc = DateTime.TryParse(
                row["updated_at_utc"], null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var updated) ? updated : DateTime.UtcNow,
            Deleted = row["deleted"] == "1",
            BlockClientId = Guid.TryParse(Column(row, "block_client_id"), out var blockId)
                ? blockId
                : null,
            Order = int.TryParse(Column(row, "sort_order"), out var order) ? order : 0,
            Attempts = int.TryParse(Column(row, "attempts"), out var attempts) ? attempts : 1,
            CompletedAttempts = int.TryParse(
                Column(row, "completed_attempts"), out var completed) ? completed : 1,
            QualityIntent = Column(row, "quality_intent"),
            LoadType = Column(row, "load_type")
        };

    /// <summary>Safe column read: a mirror created before the tracker columns
    /// existed must still load, with the new fields simply unset.</summary>
    protected static string? Column(Dictionary<string, string?> row, string key)
        => row.TryGetValue(key, out var value) ? value : null;

    protected static List<string> ParseStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static double? ParseDouble(string? value)
        => double.TryParse(value, out var parsed) ? parsed : null;

    private static List<string> ParseTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task MarkExerciseDeletedAsync(Guid clientId)
    {
        await ExecuteAsync(
            "UPDATE exercises SET deleted = 1, updated_at_utc = ? WHERE client_id = ?",
            DateTime.UtcNow.ToString("O"), clientId.ToString());
    }

    public async Task MarkWorkoutExercisesDeletedAsync(Guid workoutClientId)
    {
        await ExecuteAsync(
            "UPDATE exercises SET deleted = 1, updated_at_utc = ? WHERE workout_client_id = ?",
            DateTime.UtcNow.ToString("O"), workoutClientId.ToString());
    }

    public async Task UpsertBlockAsync(LocalBlock block)
    {
        await ExecuteAsync(
            """
            INSERT OR REPLACE INTO blocks
                (client_id, server_id, workout_client_id, sort_order, type,
                 label, rounds, updated_at_utc, deleted)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            block.ClientId.ToString(), block.ServerId?.ToString(),
            block.WorkoutClientId.ToString(), block.Order, block.Type,
            block.Label, block.Rounds, block.UpdatedAtUtc.ToString("O"),
            block.Deleted ? 1 : 0);
    }

    public async Task<LocalBlock?> GetBlockAsync(Guid clientId)
    {
        var rows = await QueryAsync(
            "SELECT * FROM blocks WHERE client_id = ?", clientId.ToString());
        return rows.Count > 0 ? MapBlock(rows[0]) : null;
    }

    public async Task<List<LocalBlock>> GetBlocksForWorkoutAsync(
        Guid workoutClientId,
        bool includeDeleted = false)
    {
        var sql = "SELECT * FROM blocks WHERE workout_client_id = ?";
        if (!includeDeleted)
        {
            sql += " AND deleted = 0";
        }

        sql += " ORDER BY sort_order ASC, updated_at_utc ASC";

        var rows = await QueryAsync(sql, workoutClientId.ToString());
        return rows.Select(MapBlock).ToList();
    }

    protected static LocalBlock MapBlock(Dictionary<string, string?> row)
        => new()
        {
            ClientId = Guid.Parse(row["client_id"]!),
            ServerId = Guid.TryParse(row["server_id"], out var serverId) ? serverId : null,
            WorkoutClientId = Guid.Parse(row["workout_client_id"]!),
            Order = int.TryParse(Column(row, "sort_order"), out var order) ? order : 0,
            Type = row["type"] ?? "Normal",
            Label = row["label"],
            Rounds = int.TryParse(Column(row, "rounds"), out var rounds)
                ? rounds
                : null,
            UpdatedAtUtc = DateTime.TryParse(
                row["updated_at_utc"], null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var updated) ? updated : DateTime.UtcNow,
            Deleted = row["deleted"] == "1"
        };

    public async Task MarkBlockDeletedAsync(Guid clientId)
    {
        await ExecuteAsync(
            "UPDATE blocks SET deleted = 1, updated_at_utc = ? WHERE client_id = ?",
            DateTime.UtcNow.ToString("O"), clientId.ToString());
    }

    public async Task MarkWorkoutBlocksDeletedAsync(Guid workoutClientId)
    {
        await ExecuteAsync(
            "UPDATE blocks SET deleted = 1, updated_at_utc = ? WHERE workout_client_id = ?",
            DateTime.UtcNow.ToString("O"), workoutClientId.ToString());
    }

    public async Task RemapEntityIdAsync(Guid clientEntityId, Guid serverEntityId)
    {
        await ExecuteAsync(
            "UPDATE workouts SET server_id = ? WHERE client_id = ?",
            serverEntityId.ToString(), clientEntityId.ToString());
        await ExecuteAsync(
            "UPDATE exercises SET server_id = ? WHERE client_id = ?",
            serverEntityId.ToString(), clientEntityId.ToString());
        await ExecuteAsync(
            "UPDATE blocks SET server_id = ? WHERE client_id = ?",
            serverEntityId.ToString(), clientEntityId.ToString());
    }
}
