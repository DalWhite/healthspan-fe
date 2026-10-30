namespace healthspan_fe_wasm_pwa.Services.Results;

/// <summary>
/// Seam between the Results page and the results API (PR #27, spec §3).
/// Page code depends only on this interface; t_3280e05c ships ResultsFixtures
/// (mock of the contract JSON) and <see cref="ResultsApiClient"/> is the real
/// HttpClient-backed client (GET /Results/{traineeId}/month + /series).
///
/// Contract re-aligned to the shipped API by t_5d7990a1: the series window is
/// trailing and anchored to the CURRENT month server-side — there is no
/// endMonth query param (spec §4), so the interface never offered the page a
/// fake anchor.
/// </summary>
public interface IResultsClient
{
    /// <summary>
    /// month = "YYYY-MM". Null only on HTTP 404; a month that exists but has
    /// no data answers with explicit not_started states, never null.
    /// </summary>
    Task<ResultsMonthResponse?> GetMonthAsync(string traineeId, string month, CancellationToken ct = default);

    /// <summary>Trailing monthly series ending at the current month (1–12; the API clamps).</summary>
    Task<ResultsSeries> GetSeriesAsync(string traineeId, int months = 6, CancellationToken ct = default);

    /// <summary>Display name for the page subtitle; t_096db2ff wires the trainee endpoint.</summary>
    Task<string?> GetTraineeNameAsync(string traineeId, CancellationToken ct = default);
}
