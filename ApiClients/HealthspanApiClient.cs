using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

public class HealthspanApiClient
{
    private readonly HttpClient _httpClient;

    public HealthspanApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<BloodTestSummaryResource>> GetBloodTestsAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<BloodTestSummaryResource>>(HealthRoutes.BloodTests, cancellationToken)
              ?? new List<BloodTestSummaryResource>();

    public Task<GetBloodTestResource?> GetBloodTestAsync(Guid bloodTestId, CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<GetBloodTestResource>($"{HealthRoutes.BloodTests}/{bloodTestId}", cancellationToken);

    public async Task<IReadOnlyList<BloodTestSummaryResource>> GetPendingReviewBloodTestsAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<BloodTestSummaryResource>>(HealthRoutes.BloodTestsPendingReview, cancellationToken)
              ?? new List<BloodTestSummaryResource>();

    public async Task<BloodTestUploadResource>
        UploadBloodReportAsync(
            IBrowserFile file,
            BloodTestUploadRequest request,
            CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream(20 * 1024 * 1024, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var path = BuildPath(HealthRoutes.BloodTestUpload, new Dictionary<string, string?>
        {
            ["testDate"] = request.TestDate?.ToString("O"),
            ["labName"] = request.LabName
        });

        var response = await _httpClient.PostAsync(path, content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content
                   .ReadFromJsonAsync<BloodTestUploadResource>(
                       cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException(
                   "The upload response was incomplete.");
    }

    public async Task CreateManualBloodTestAsync(AddBloodTestResource request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(HealthRoutes.BloodTestManual, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ReviewBloodTestAsync(Guid bloodTestId, ReviewBloodTestResource request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{HealthRoutes.BloodTests}/{bloodTestId}/review", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteBloodTestAsync(
        Guid bloodTestId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"{HealthRoutes.BloodTests}/{bloodTestId}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<BloodTestDocumentContent?> GetBloodTestDocumentAsync(
        Guid bloodTestId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"{HealthRoutes.BloodTests}/{bloodTestId}/document",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return new BloodTestDocumentContent
        {
            Content = await response.Content.ReadAsByteArrayAsync(
                cancellationToken),
            ContentType = response.Content.Headers.ContentType?.MediaType
                          ?? "application/octet-stream"
        };
    }

    public async Task<IReadOnlyList<LatestMarkerSnapshotResource>> GetLatestMarkersAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<LatestMarkerSnapshotResource>>(HealthRoutes.BiomarkerSnapshot, cancellationToken)
              ?? new List<LatestMarkerSnapshotResource>();

    public Task<GetMarkerTrendResource?> GetMarkerTrendAsync(string metricKey, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        var path = BuildPath($"{HealthRoutes.BiomarkerTrendPrefix}/{Uri.EscapeDataString(metricKey)}/trends", new Dictionary<string, string?>
        {
            ["fromUtc"] = fromUtc?.ToString("O"),
            ["toUtc"] = toUtc?.ToString("O")
        });

        return _httpClient.GetFromJsonAsync<GetMarkerTrendResource>(path, cancellationToken);
    }

    public async Task<IReadOnlyList<GalpinAdaptationCatalogItemResource>> GetGalpinCatalogAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<GalpinAdaptationCatalogItemResource>>(HealthRoutes.GalpinCatalog, cancellationToken)
              ?? new List<GalpinAdaptationCatalogItemResource>();

    public async Task<IReadOnlyList<GetGalpinAssessmentResource>> GetGalpinAssessmentsAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<GetGalpinAssessmentResource>>(HealthRoutes.GalpinAssessments, cancellationToken)
              ?? new List<GetGalpinAssessmentResource>();

    public Task<GetGalpinAssessmentResource?> GetLatestGalpinAssessmentAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<GetGalpinAssessmentResource>(HealthRoutes.GalpinLatest, cancellationToken);

    public Task<GetGalpinAdaptationTrendResource?> GetGalpinTrendAsync(GalpinAdaptationType adaptation, CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<GetGalpinAdaptationTrendResource>($"{HealthRoutes.GalpinTrendPrefix}/{(int)adaptation}", cancellationToken);

    public async Task CreateGalpinAssessmentAsync(AddGalpinAssessmentResource request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(HealthRoutes.GalpinAssessments, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateGalpinAssessmentAsync(
        Guid assessmentId,
        AddGalpinAssessmentResource request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"{HealthRoutes.GalpinAssessments}/{assessmentId}",
            request,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteGalpinAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"{HealthRoutes.GalpinAssessments}/{assessmentId}",
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<GetHealthDashboardSummaryResource?> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<GetHealthDashboardSummaryResource>(HealthRoutes.DashboardSummary, cancellationToken);

    public async Task<HealthProfileResource> GetHealthProfileAsync(
        CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<HealthProfileResource>(
               HealthRoutes.HealthProfile,
               cancellationToken)
           ?? new HealthProfileResource();

    public async Task<HealthProfileResource> UpdateHealthProfileAsync(
        UpdateHealthProfileResource request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync(
            HealthRoutes.HealthProfile,
            request,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<HealthProfileResource>(
                       cancellationToken: cancellationToken)
               ?? new HealthProfileResource();
    }

    private static string BuildPath(string basePath, IReadOnlyDictionary<string, string?> query)
    {
        var items = query
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}")
            .ToArray();

        return items.Length == 0 ? basePath : $"{basePath}?{string.Join("&", items)}";
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content
            .ReadAsStringAsync(cancellationToken);
        string? code = null;
        string? title = null;
        string? detail = null;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var json =
                    JsonDocument.Parse(responseBody);
                if (json.RootElement.ValueKind
                    == JsonValueKind.Object)
                {
                    code = ReadJsonString(
                        json.RootElement,
                        "code");
                    title = ReadJsonString(
                        json.RootElement,
                        "title");
                    detail = ReadJsonString(
                        json.RootElement,
                        "detail");
                }
                else if (json.RootElement.ValueKind
                         == JsonValueKind.String)
                {
                    detail =
                        json.RootElement.GetString();
                }
            }
            catch (JsonException)
            {
                detail = responseBody.Trim();
            }
        }

        throw new HealthApiException(
            detail
            ?? title
            ?? "The health request could not be completed.",
            code,
            response.StatusCode);
    }

    private static string? ReadJsonString(
        JsonElement element,
        string propertyName)
        => element.TryGetProperty(
               propertyName,
               out var property)
           && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}

public sealed class HealthApiException : HttpRequestException
{
    public HealthApiException(
        string message,
        string? code,
        HttpStatusCode statusCode)
        : base(message, null, statusCode)
    {
        Code = code;
    }

    public string? Code { get; }
}
