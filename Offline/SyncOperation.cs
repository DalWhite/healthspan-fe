namespace Healthspan.Offline;

/// <summary>
/// A pending local-first write waiting to be replayed against the API.
/// OpId is client-generated and doubles as the server-side dedupe key.
/// </summary>
public sealed class SyncOperation
{
    public Guid OpId { get; init; } = Guid.NewGuid();
    public required string Entity { get; init; }
    public required string Action { get; init; }
    public Guid? ClientEntityId { get; init; }
    public required string Payload { get; init; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}

public static class SyncEntities
{
    public const string Workout = "workout";
    public const string Exercise = "exercise";
    public const string WorkoutBlock = "workout_block";
}

public static class SyncActions
{
    public const string Create = "create";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Duplicate = "duplicate";
}
