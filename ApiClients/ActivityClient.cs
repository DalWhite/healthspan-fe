using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Client for /api/activities following the HealthspanApiClient pattern.
/// Contract: api-contract-activities-t_0e23bbe6.md — canonical dimension
/// strings, occurredOnUtc in UTC, per-type params inside `metadata`,
/// ProblemDetails errors surfaced as <see cref="ActivityApiException.FieldErrors"/>
/// so the page can render them inline per field.
/// </summary>
public class ActivityClient
{
    private readonly HttpClient _httpClient;

    public ActivityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WellbeingActivityResource> CreateActivityAsync(
        WellbeingActivityWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(ActivityRoutes.Activities, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<WellbeingActivityResource>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("The activity response was incomplete.");
    }

    public Task<WellbeingActivityResource?> GetActivityAsync(
        Guid activityId,
        CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<WellbeingActivityResource>(
               $"{ActivityRoutes.Activities}/{activityId}",
               cancellationToken);

    public async Task UpdateActivityAsync(
        Guid activityId,
        WellbeingActivityWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"{ActivityRoutes.Activities}/{activityId}",
            request,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        string? code = null;
        string? title = null;
        string? detail = null;
        Dictionary<string, string>? fieldErrors = null;

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

                    if (json.RootElement.TryGetProperty("errors", out var errors)
                        && errors.ValueKind == JsonValueKind.Object)
                    {
                        fieldErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var property in errors.EnumerateObject())
                        {
                            var message = property.Value.ValueKind == JsonValueKind.Array
                                ? property.Value.EnumerateArray()
                                    .FirstOrDefault(e => e.ValueKind == JsonValueKind.String)
                                    .GetString()
                                : property.Value.GetString();

                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                fieldErrors[property.Name] = message;
                            }
                        }
                    }
                }
                else if (json.RootElement.ValueKind == JsonValueKind.String)
                {
                    detail = json.RootElement.GetString();
                }
            }
            catch (JsonException)
            {
                detail = responseBody.Trim();
            }
        }

        throw new ActivityApiException(
            detail ?? title ?? "The activity request could not be completed.",
            code,
            response.StatusCode,
            fieldErrors);
    }

    private static string? ReadJsonString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property)
           && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

public sealed class ActivityApiException : HttpRequestException
{
    public ActivityApiException(
        string message,
        string? code,
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, string>? fieldErrors = null)
        : base(message, null, statusCode)
    {
        Code = code;
        FieldErrors = fieldErrors;
    }

    public string? Code { get; }

    /// <summary>API field name (camelCase, e.g. "temperatureCelsius") → first error message.</summary>
    public IReadOnlyDictionary<string, string>? FieldErrors { get; }
}
