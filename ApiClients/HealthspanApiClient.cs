using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

public class HealthspanApiClient
{
    private readonly HttpClient _httpClient;

    public HealthspanApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<GetBloodTestResource>> GetBloodTestsAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<GetBloodTestResource>>(HealthRoutes.BloodTests, cancellationToken)
              ?? new List<GetBloodTestResource>();

    public Task<GetBloodTestResource?> GetBloodTestAsync(Guid bloodTestId, CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<GetBloodTestResource>($"{HealthRoutes.BloodTests}/{bloodTestId}", cancellationToken);

    public async Task<IReadOnlyList<GetBloodTestResource>> GetPendingReviewBloodTestsAsync(CancellationToken cancellationToken = default)
          => await _httpClient.GetFromJsonAsync<List<GetBloodTestResource>>(HealthRoutes.BloodTestsPendingReview, cancellationToken)
              ?? new List<GetBloodTestResource>();

    public async Task UploadBloodReportAsync(IBrowserFile file, BloodTestUploadRequest request, CancellationToken cancellationToken = default)
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
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateManualBloodTestAsync(AddBloodTestResource request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(HealthRoutes.BloodTestManual, request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReviewBloodTestAsync(Guid bloodTestId, ReviewBloodTestResource request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{HealthRoutes.BloodTests}/{bloodTestId}/review", request, cancellationToken);
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();

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
}
