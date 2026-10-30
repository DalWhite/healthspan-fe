using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace healthspan_fe_wasm_pwa.Services.Results;

/// <summary>
/// Real HttpClient-backed IResultsClient (t_5d7990a1). Pure fetch-and-render
/// client: it deserializes the API contract into the page DTOs and performs
/// ZERO business math — no recomputation, rounding, diffing or defaults-for-
/// nulls anywhere (spec results-kpis-ux-t_33c412cf.md §6).
///
/// Error contract (so the page can choose retry-inline vs empty-state):
///  • 404 on month        → null (interface contract).
///  • 403                 → <see cref="ResultsApiException"/> Forbidden —
///    [TraineeAccess] answers 403 without body for foreign/unknown trainees;
///    the page must NOT retry silently, it shows the no-access state.
///  • 400 (month_invalid / month_in_future), 5xx, incomplete body →
///    <see cref="ResultsApiException"/> carrying the ProblemDetails `code`.
///  • network/timeout     → HttpRequestException propagates as-is (page shows
///    retry-inline, per app-shell rules).
/// </summary>
public sealed class ResultsApiClient : IResultsClient
{
    private readonly HttpClient _httpClient;

    public ResultsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ResultsMonthResponse?> GetMonthAsync(
        string traineeId,
        string month,
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(
            BuildMonthPath(traineeId, month),
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);
        return await response.Content
                   .ReadFromJsonAsync<ResultsMonthResponse>(cancellationToken: ct)
               ?? throw new ResultsApiException(
                   "The results month response was incomplete.",
                   code: null,
                   response.StatusCode);
    }

    public async Task<ResultsSeries> GetSeriesAsync(
        string traineeId,
        int months = 6,
        CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(
            $"{ResultsRoutes.Series(traineeId)}?months={months}",
            ct);

        await EnsureSuccessAsync(response, ct);
        return await response.Content
                   .ReadFromJsonAsync<ResultsSeries>(cancellationToken: ct)
               ?? throw new ResultsApiException(
                   "The results series response was incomplete.",
                   code: null,
                   response.StatusCode);
    }

    /// <summary>
    /// Out of scope for t_5d7990a1 (client card covers month + series only):
    /// returns null so the page keeps its subtitle fallback. t_096db2ff wires
    /// the real trainee-name source when it swaps the DI registration.
    /// </summary>
    public Task<string?> GetTraineeNameAsync(string traineeId, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    private static string BuildMonthPath(string traineeId, string month)
        => $"{ResultsRoutes.Month(traineeId)}?month={Uri.EscapeDataString(month)}";

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? code = null;
        string? title = null;
        string? detail = null;

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var json = JsonDocument.Parse(responseBody);
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    code = ReadJsonString(json.RootElement, "code");
                    title = ReadJsonString(json.RootElement, "title");
                    detail = ReadJsonString(json.RootElement, "detail");
                }
            }
            catch (JsonException)
            {
                // non-JSON error body: the status code alone is enough to route
                // the UI (403 → no-access state, 5xx/network → retry inline).
            }
        }

        throw new ResultsApiException(
            detail ?? title ?? $"The results request failed ({(int)response.StatusCode}).",
            code,
            response.StatusCode);
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property)
           && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

/// <summary>Route table for the Results API (controller route is /Results).</summary>
public static class ResultsRoutes
{
    public static string Month(string traineeId)
        => $"/Results/{Uri.EscapeDataString(traineeId)}/month";

    public static string Series(string traineeId)
        => $"/Results/{Uri.EscapeDataString(traineeId)}/series";
}

public sealed class ResultsApiException : HttpRequestException
{
    public ResultsApiException(string message, string? code, HttpStatusCode statusCode)
        : base(message, null, statusCode)
    {
        Code = code;
    }

    /// <summary>ProblemDetails `code` from the API (month_invalid, month_in_future,
    /// results_load_failed…) when present.</summary>
    public string? Code { get; }

    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
