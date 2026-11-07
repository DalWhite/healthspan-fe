using System;

namespace Healthspan.Offline;

/// <summary>
/// Hydration state of one catalog mirror snapshot (mirror_meta). The rows
/// themselves live in mirror_catalog, written atomically per page; this state
/// is what makes a partial hydration resumable and a cancellation durable
/// across a cold start. The resume offset is valid because page order is the
/// deterministic Name→Id order of the pagination contract.
/// </summary>
public sealed record OfflineMirrorState
{
    public const string Hydrating = "hydrating";
    public const string Paused = "paused";
    public const string Complete = "complete";

    /// <summary>Mirror key, e.g. "exercise_types" or "muscles".</summary>
    public required string Kind { get; init; }

    /// <summary>One of the status constants above.</summary>
    public required string Status { get; init; }

    /// <summary>Row count the server reported for the snapshot (X-Total-Count).</summary>
    public int TargetTotal { get; init; }

    /// <summary>Fetch offset of the last fully stored chunk; where a resume starts.</summary>
    public int HydratedThrough { get; init; }

    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;

    public bool IsComplete => Status == Complete;
}
