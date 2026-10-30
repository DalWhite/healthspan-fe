namespace healthspan_fe_wasm_pwa.Services.Results;

/// <summary>
/// Mock IResultsClient backed by fixtures of the API contract JSON (page code
/// will not change when ResultsApiClient is registered in its place). Data
/// mirrors the accepted prototype docs/design/prototypes/results-kpis-t_33c412cf.html
/// frame A (2026-09) and its series; dedicated months demo each state for
/// tests/QA:
///   2026-09  all ok (prototype)        2026-08  weight not_started (Aug gap)
///   2026-07  weight plateau (± 0,0)    2026-06  strength insufficient_data
///   2026-02  weight insufficient_data  2026-01  weight current_only
///   2025-12  everything not_started
/// Month payloads and series points are independent fixtures and do not claim
/// numeric coherence between a month card and its series point.
/// Shapes follow the shipped API contract (PR #27, realigned by t_5d7990a1):
/// double numerics, no plannedSessions, series arrays aligned index-wise with
/// the top-level Months array.
/// </summary>
public sealed class ResultsFixtures : IResultsClient
{
    public const string TraineeId = "t-ana";
    public const string TraineeName = "Ana López";
    public const string FirstMonth = "2025-12";
    public const string LastMonth = "2026-09";
    public static readonly string[] SeriesMonths =
        ["2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09"];

    private static readonly Dictionary<string, ResultsMonthResponse> Months = new()
    {
        ["2026-09"] = Month("2026-09",
            new ResultsConsistency(ResultsState.Ok, 0.75, -10, 9),
            new ResultsWeight(ResultsState.Ok, 81.4, -0.6),
            new ResultsStrength(ResultsState.Ok, 4.2,
            [
                Type(1, "Sentadilla", "squat", "kg", 2.5),
                Type(2, "Peso muerto", "deadlift", "kg", 0.0),
                Type(3, "Press banca", "chest-press", "reps", 1.8),
                Type(4, "Dominada", "pull-up", "%", 6.0),
                Type(5, "Muscle-up", "muscle-up", "%", -1.2),
            ])),
        ["2026-08"] = Month("2026-08",
            new ResultsConsistency(ResultsState.Ok, 0.85, 27, 10),
            new ResultsWeight(ResultsState.NotStarted, null, null),
            new ResultsStrength(ResultsState.Ok, 3.5,
            [Type(1, "Sentadilla", "squat", "kg", 2.0), Type(2, "Peso muerto", "deadlift", "kg", -1.0)])),
        ["2026-07"] = Month("2026-07",
            new ResultsConsistency(ResultsState.Ok, 0.58, -3, 7),
            // legit plateau: delta 0 rendered as flat chip, never red/green (E9)
            new ResultsWeight(ResultsState.Ok, 81.7, 0.0),
            new ResultsStrength(ResultsState.Ok, 2.6,
            [Type(1, "Sentadilla", "squat", "kg", 2.5)])),
        ["2026-06"] = Month("2026-06",
            new ResultsConsistency(ResultsState.Ok, 0.61, 6, 7),
            new ResultsWeight(ResultsState.Ok, 81.9, -0.1),
            new ResultsStrength(ResultsState.InsufficientData, null, [])),
        ["2026-05"] = Month("2026-05",
            new ResultsConsistency(ResultsState.Ok, 0.55, 13, 6),
            new ResultsWeight(ResultsState.Ok, 82.0, -0.6),
            new ResultsStrength(ResultsState.Ok, 1.4,
            [Type(1, "Sentadilla", "squat", "kg", 2.5)])),
        ["2026-04"] = Month("2026-04",
            new ResultsConsistency(ResultsState.Ok, 0.42, null, 4),
            new ResultsWeight(ResultsState.Ok, 82.6, -0.4),
            new ResultsStrength(ResultsState.Ok, 0.6,
            [Type(1, "Sentadilla", "squat", "kg", 2.5)])),
        ["2026-02"] = Month("2026-02",
            new ResultsConsistency(ResultsState.Ok, 0.40, 5, 5),
            new ResultsWeight(ResultsState.InsufficientData, 79.8, null),
            new ResultsStrength(ResultsState.Ok, 0.3, [])),
        ["2026-01"] = Month("2026-01",
            new ResultsConsistency(ResultsState.Ok, 0.33, null, 4),
            // only one registration that month -> current_only, hero kept (E2)
            new ResultsWeight(ResultsState.CurrentOnly, 79.5, null),
            new ResultsStrength(ResultsState.InsufficientData, null, [])),
        ["2025-12"] = Month("2025-12",
            // API answers activeSessions as a plain int (0 when not_started);
            // score/deltaPp stay null and are never replaced by 0.
            new ResultsConsistency(ResultsState.NotStarted, null, null, 0),
            new ResultsWeight(ResultsState.NotStarted, null, null),
            new ResultsStrength(ResultsState.NotStarted, null, [])),
    };

    public Task<ResultsMonthResponse?> GetMonthAsync(string traineeId, string month, CancellationToken ct = default) =>
        Task.FromResult(Months.TryGetValue(month, out var m) ? m with { TraineeId = traineeId } : null);

    public Task<ResultsSeries> GetSeriesAsync(string traineeId, int months = 6, CancellationToken ct = default)
    {
        // Trailing window over the demo months (the real API anchors the window
        // at the current month; SeriesMonths is the fixture's equivalent).
        var window = months >= SeriesMonths.Length
            ? SeriesMonths
            : SeriesMonths[^months..];

        var consistency = new Dictionary<string, ResultsConsistency>
        {
            ["2026-04"] = new(ResultsState.Ok, 0.42, null, 4),
            ["2026-05"] = new(ResultsState.Ok, 0.55, 13, 6),
            ["2026-06"] = new(ResultsState.Ok, 0.61, 6, 7),
            ["2026-07"] = new(ResultsState.Ok, 0.58, -3, 7),
            ["2026-08"] = new(ResultsState.Ok, 0.85, 27, 10),
            ["2026-09"] = new(ResultsState.Ok, 0.75, -10, 9),
        };
        var weight = new Dictionary<string, ResultsWeight>
        {
            ["2026-04"] = new(ResultsState.Ok, 82.6, null),
            ["2026-05"] = new(ResultsState.Ok, 82.0, -0.6),
            ["2026-06"] = new(ResultsState.Ok, 81.9, -0.1),
            ["2026-07"] = new(ResultsState.Ok, 81.7, -0.2),
            // series con hueco (spec §4): agosto sin lectura, sparkline discontinua
            ["2026-08"] = new(ResultsState.InsufficientData, null, null),
            ["2026-09"] = new(ResultsState.Ok, 81.4, -0.3),
        };
        var strengthIndex = new Dictionary<string, ResultsStrengthPoint>
        {
            ["2026-04"] = new("2026-04", ResultsState.Ok, 0.6),
            ["2026-05"] = new("2026-05", ResultsState.Ok, 1.4),
            ["2026-06"] = new("2026-06", ResultsState.InsufficientData, null),
            ["2026-07"] = new("2026-07", ResultsState.Ok, 2.6),
            ["2026-08"] = new("2026-08", ResultsState.Ok, 3.5),
            ["2026-09"] = new("2026-09", ResultsState.Ok, 4.2),
        };

        return Task.FromResult(new ResultsSeries(
            traineeId, "2026-10-01T06:00:00Z",
            [.. window],
            [.. window.Select(m => consistency[m])],
            [.. window.Select(m => weight[m])],
            new ResultsStrengthSeries(
                [.. window.Select(m => strengthIndex[m])],
                [
                    new ResultsStrengthTypeSeries(1, "Sentadilla", "squat", "kg",
                    [
                        new ResultsStrengthTypePoint("2026-04", ResultsState.Ok, 2.5, null),
                        new ResultsStrengthTypePoint("2026-05", ResultsState.Ok, 2.5, null),
                        new ResultsStrengthTypePoint("2026-06", ResultsState.InsufficientData, null, null),
                        new ResultsStrengthTypePoint("2026-07", ResultsState.Ok, 2.5, null),
                        new ResultsStrengthTypePoint("2026-08", ResultsState.Ok, 2.0, null),
                        new ResultsStrengthTypePoint("2026-09", ResultsState.Ok, 2.5, null),
                    ]),
                ])));
    }

    public Task<string?> GetTraineeNameAsync(string traineeId, CancellationToken ct = default) =>
        Task.FromResult(traineeId == TraineeId ? TraineeName : null);

    private static ResultsMonthResponse Month(string month, ResultsConsistency c, ResultsWeight w, ResultsStrength s) =>
        new(TraineeId, "2026-10-01T06:00:00Z", month, c, w, s);

    private static ResultsStrengthType Type(int typeId, string name, string key, string unit, double delta) =>
        new(typeId, name, key, unit, ResultsState.Ok, delta, unit == "%" ? delta : null);
}
