using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Tests the HealthspanApiClient wrapper methods against a mock HTTP handler.
/// Validates that routes, serialization, and error handling work correctly.
/// </summary>
public class HealthspanApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HealthspanApiClient _client;

    public HealthspanApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");
        _client = new HealthspanApiClient(httpClient);
    }

    [Fact]
    public async Task GetBloodTestsAsync_Returns_List()
    {
        _mockHttp.When("/BloodTests")
            .Respond("application/json", """
                [
                    { "id": "00000000-0000-0000-0000-000000000001", "testDate": "2025-01-01", "labName": "Lab1", "markers": [] }
                ]
            """);

        var result = await _client.GetBloodTestsAsync();

        result.Should().HaveCount(1);
        result[0].LabName.Should().Be("Lab1");
    }

    [Fact]
    public async Task GetBloodTestsAsync_Returns_Empty_On_Null()
    {
        _mockHttp.When("/BloodTests")
            .Respond("application/json", "null");

        var result = await _client.GetBloodTestsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_Returns_Summary()
    {
        _mockHttp.When("/Dashboard/summary")
            .Respond("application/json", """
                {
                    "recentWorkoutCount": 5,
                    "bloodTestCount": 2,
                    "outOfRangeMarkersInLatestBloodTest": 1
                }
            """);

        var result = await _client.GetDashboardSummaryAsync();

        result.Should().NotBeNull();
        result!.RecentWorkoutCount.Should().Be(5);
        result.BloodTestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetGalpinCatalogAsync_Returns_Catalog_Items()
    {
        _mockHttp.When("/GalpinAssessments/catalog")
            .Respond("application/json", """
                [
                    { "adaptation": 0, "name": "Strength", "description": "Max force" }
                ]
            """);

        var result = await _client.GetGalpinCatalogAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLatestMarkersAsync_Returns_Empty_When_No_Data()
    {
        _mockHttp.When("/BloodTests/markers/latest")
            .Respond("application/json", "[]");

        var result = await _client.GetLatestMarkersAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateManualBloodTestAsync_Posts_Successfully()
    {
        _mockHttp.When("/BloodTests/manual")
            .Respond(HttpStatusCode.Created);

        var request = new AddBloodTestResource
        {
            TestDate = DateTime.UtcNow,
            LabName = "Test Lab",
            Markers = new List<BloodTestMarkerResource>
            {
                new()
                {
                    Name = "Glucose",
                    Value = 95,
                    Unit = "mg/dL",
                    Status = MarkerStatus.InRange
                }
            }
        };

        var act = () => _client.CreateManualBloodTestAsync(request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetBloodTestAsync_Returns_SingleTest()
    {
        var testId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _mockHttp.When($"/BloodTests/{testId}")
            .Respond("application/json", """
                {
                    "id": "00000000-0000-0000-0000-000000000001",
                    "testDate": "2025-01-01",
                    "labName": "Detail Lab",
                    "markers": [{ "name": "HDL", "value": 55, "unit": "mg/dL", "status": 1 }]
                }
            """);

        var result = await _client.GetBloodTestAsync(testId);

        result.Should().NotBeNull();
        result!.LabName.Should().Be("Detail Lab");
        result.Markers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPendingReviewBloodTestsAsync_Returns_List()
    {
        _mockHttp.When("/BloodTests/pending-review")
            .Respond("application/json", """
                [
                    { "id": "00000000-0000-0000-0000-000000000010", "testDate": "2025-06-01", "requiresReview": true, "markers": [] }
                ]
            """);

        var result = await _client.GetPendingReviewBloodTestsAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLatestMarkersAsync_Returns_Markers()
    {
        _mockHttp.When("/BloodTests/markers/latest")
            .Respond("application/json", """
                [
                    { "metricKey": "glucose", "displayName": "Glucose", "value": 90, "unit": "mg/dL", "status": 1 }
                ]
            """);

        var result = await _client.GetLatestMarkersAsync();

        result.Should().HaveCount(1);
        result[0].MetricKey.Should().Be("glucose");
    }

    [Fact]
    public async Task GetGalpinAssessmentsAsync_Returns_List()
    {
        _mockHttp.When("/GalpinAssessments")
            .Respond("application/json", "[]");

        var result = await _client.GetGalpinAssessmentsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestGalpinAssessmentAsync_Throws_When_NotFound()
    {
        _mockHttp.When("/GalpinAssessments/latest")
            .Respond(HttpStatusCode.NotFound);

        var act = () => _client.GetLatestGalpinAssessmentAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ReviewBloodTestAsync_Posts_Successfully()
    {
        var testId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        _mockHttp.When($"/BloodTests/{testId}/review")
            .Respond(HttpStatusCode.OK);

        var review = new ReviewBloodTestResource
        {
            TestDate = DateTime.UtcNow,
            LabName = "Review Lab",
            Markers = new List<BloodTestMarkerResource>
            {
                new() { Name = "Glucose", Value = 92, Unit = "mg/dL", Status = MarkerStatus.InRange }
            }
        };

        var act = () => _client.ReviewBloodTestAsync(testId, review);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_Throws_On_Error()
    {
        _mockHttp.When("/Dashboard/summary")
            .Respond(HttpStatusCode.InternalServerError);

        var act = () => _client.GetDashboardSummaryAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
