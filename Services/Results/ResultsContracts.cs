namespace healthspan_fe_wasm_pwa.Services.Results;

/// <summary>
/// DTOs mirroring the ACCEPTED API contract 1:1 — Controllers/ApiContracts/
/// Results/ResultsResources.cs on PR #27 (head cc55e1e, QA-signed), served at
/// GET /Results/{traineeId}/month?month=YYYY-MM and
/// GET /Results/{traineeId}/series?months=N (spec t_358e09e9 §3, camelCase).
///
/// FE rule (spec results-kpis-ux-t_33c412cf.md §6): these values are rendered
/// AS-IS. No recomputation, no rounding, no client-side math on KPI fields.
/// Formatting (sign, decimal comma, NBSP, ×100 for percent display) is the
/// only allowed transformation — see ResultsFormat.
///
/// Shape notes fixed by the shipped API (they differ from early wireframes):
///  • consistency carries score/deltaPp/activeSessions — NO plannedSessions;
///  • series arrays align positionally with the top-level Months array, so the
///    individual points do not repeat the month except where the API does;
///  • strength series is nested under strength.{indexPct, byType};
///  • every numeric is double (PR#25 decimal→double lesson); nulls are
///    explicit and never replaced by 0.
/// </summary>
public sealed record ResultsMonthResponse(
    string TraineeId,
    string GeneratedAtUtc,
    string Month,
    ResultsConsistency Consistency,
    ResultsWeight Weight,
    ResultsStrength Strength);

public sealed record ResultsConsistency(
    string State,
    double? Score,        // 0..1, rendered as "75 %" (×100 is display-only)
    double? DeltaPp,      // already resolved by the API, never re-diffed
    int ActiveSessions);

public sealed record ResultsWeight(
    string State,
    double? LastKg,
    double? DeltaKg);

public sealed record ResultsStrength(
    string State,
    double? IndexPct,     // signed, already resolved
    IReadOnlyList<ResultsStrengthType> ByType);

public sealed record ResultsStrengthType(
    int TypeId,
    string Name,          // display name straight from the API
    string Key,           // stable icon/label key: squat, deadlift, chest-press…
    string Unit,          // kg | reps | %  — units are NEVER mixed across types
    string State,
    double? Delta,        // signed, in Unit
    double? DeltaPct);

/// <summary>
/// GET /Results/{id}/series?months=N (spec §4): Months is the ascending window
/// ending at the CURRENT month (server-anchored; the API has no endMonth
/// param). Consistency/Weight lists align index-wise with Months.
/// </summary>
public sealed record ResultsSeries(
    string TraineeId,
    string GeneratedAtUtc,
    IReadOnlyList<string> Months,
    IReadOnlyList<ResultsConsistency> Consistency,
    IReadOnlyList<ResultsWeight> Weight,
    ResultsStrengthSeries Strength);

public sealed record ResultsStrengthSeries(
    IReadOnlyList<ResultsStrengthPoint> IndexPct,
    IReadOnlyList<ResultsStrengthTypeSeries> ByType);

public sealed record ResultsStrengthPoint(
    string Month,
    string State,
    double? Value);

public sealed record ResultsStrengthTypeSeries(
    int TypeId,
    string Name,
    string Key,
    string Unit,
    IReadOnlyList<ResultsStrengthTypePoint> Points);

public sealed record ResultsStrengthTypePoint(
    string Month,
    string State,
    double? Delta,
    double? DeltaPct);

/// <summary>State enum values from the API contract (rendered, never derived).</summary>
public static class ResultsState
{
    public const string Ok = "ok";
    public const string NotStarted = "not_started";
    public const string CurrentOnly = "current_only";
    public const string InsufficientData = "insufficient_data";
}
