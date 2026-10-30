using System.Net;
using System.Text.Json;
using FluentAssertions;
using healthspan_fe_wasm_pwa.Services.Results;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// ResultsApiClient against a mock HTTP handler (t_5d7990a1). JSON bodies are
/// the contract from healthspan-api PR #27 (Controllers/ApiContracts/Results/
/// ResultsResources.cs, spec t_358e09e9 §3) — camelCase, explicit nulls,
/// double numerics. Tests pin routes, query params, deserialization fidelity
/// (no FE math: values arrive exactly as sent) and the error contract the UI
/// needs: 404→null, 403→typed Forbidden, 4xx/5xx→typed with ProblemDetails
/// code, network→HttpRequestException for retry-inline.
/// </summary>
public class ResultsApiClientTests
{
    private const string TraineeId = "t-ana";
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ResultsApiClient _client;

    public ResultsApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");
        _client = new ResultsApiClient(httpClient);
    }

    private const string MonthJson = """
        {
          "traineeId": "t-ana",
          "generatedAtUtc": "2026-10-01T06:00:00Z",
          "month": "2026-09",
          "consistency": { "state": "ok", "score": 0.75, "deltaPp": -10, "activeSessions": 9 },
          "weight": { "state": "ok", "lastKg": 81.4, "deltaKg": -0.6 },
          "strength": {
            "state": "ok",
            "indexPct": 4.2,
            "byType": [
              { "typeId": 1, "name": "Sentadilla", "key": "squat", "unit": "kg",
                "state": "ok", "delta": 2.5, "deltaPct": 3.1 }
            ]
          }
        }
        """;

    private const string SeriesJson = """
        {
          "traineeId": "t-ana",
          "generatedAtUtc": "2026-10-01T06:00:00Z",
          "months": ["2026-04", "2026-05", "2026-06"],
          "consistency": [
            { "state": "ok", "score": 0.42, "deltaPp": null, "activeSessions": 4 },
            { "state": "ok", "score": 0.55, "deltaPp": 13, "activeSessions": 6 },
            { "state": "ok", "score": 0.61, "deltaPp": 6, "activeSessions": 7 }
          ],
          "weight": [
            { "state": "ok", "lastKg": 82.6, "deltaKg": null },
            { "state": "ok", "lastKg": 82.0, "deltaKg": -0.6 },
            { "state": "insufficient_data", "lastKg": null, "deltaKg": null }
          ],
          "strength": {
            "indexPct": [
              { "month": "2026-04", "state": "ok", "value": 0.6 },
              { "month": "2026-05", "state": "ok", "value": 1.4 },
              { "month": "2026-06", "state": "insufficient_data", "value": null }
            ],
            "byType": [
              { "typeId": 1, "name": "Sentadilla", "key": "squat", "unit": "kg",
                "points": [
                  { "month": "2026-04", "state": "ok", "delta": 2.5, "deltaPct": null },
                  { "month": "2026-05", "state": "ok", "delta": 2.5, "deltaPct": null },
                  { "month": "2026-06", "state": "insufficient_data", "delta": null, "deltaPct": null }
                ] }
            ]
          }
        }
        """;

    [Fact]
    public async Task GetMonthAsync_Gets_Contract_Route_And_Deserializes_AsIs()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .WithQueryString("month", "2026-09")
            .Respond("application/json", MonthJson);

        var result = await _client.GetMonthAsync(TraineeId, "2026-09");

        result.Should().NotBeNull();
        result!.Month.Should().Be("2026-09");
        result.Consistency.State.Should().Be(ResultsState.Ok);
        result.Consistency.Score.Should().Be(0.75);
        result.Consistency.DeltaPp.Should().Be(-10);
        result.Consistency.ActiveSessions.Should().Be(9);
        result.Weight.LastKg.Should().Be(81.4);
        result.Weight.DeltaKg.Should().Be(-0.6);
        result.Strength.IndexPct.Should().Be(4.2);
        result.Strength.ByType[0].TypeId.Should().Be(1);
        result.Strength.ByType[0].Name.Should().Be("Sentadilla");
        result.Strength.ByType[0].Delta.Should().Be(2.5);
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetMonthAsync_Returns_Null_On_404()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .Respond(HttpStatusCode.NotFound);

        var result = await _client.GetMonthAsync(TraineeId, "2026-09");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMonthAsync_Throws_Typed_Forbidden_On_403()
    {
        // [TraineeAccess] answers 403 with no body for foreign trainees.
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .Respond(HttpStatusCode.Forbidden);

        var ex = await Assert.ThrowsAsync<ResultsApiException>(
            () => _client.GetMonthAsync(TraineeId, "2026-09"));

        ex.IsForbidden.Should().BeTrue();
        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMonthAsync_Surface_ProblemDetails_Code_On_400()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .Respond(HttpStatusCode.BadRequest, "application/problem+json", """
                { "type": "https://httpspan/validation", "title": "Validation failed",
                  "status": 400, "code": "month_in_future" }
                """);

        var ex = await Assert.ThrowsAsync<ResultsApiException>(
            () => _client.GetMonthAsync(TraineeId, "2099-12"));

        ex.Code.Should().Be("month_in_future");
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.IsForbidden.Should().BeFalse();
    }

    [Fact]
    public async Task GetMonthAsync_Escapes_Month_Query_Param()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t%2Fx/month")
            .WithQueryString("month", "2026-09")
            .Respond("application/json", MonthJson);

        var result = await _client.GetMonthAsync("t/x", "2026-09");

        result.Should().NotBeNull();
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetMonthAsync_Propagates_Network_Failure_For_Retry_Inline()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .Throw(new HttpRequestException("Network error"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => _client.GetMonthAsync(TraineeId, "2026-09"));
    }

    [Fact]
    public async Task GetSeriesAsync_Gets_Trailing_Window_And_Aligned_Arrays()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/series")
            .WithQueryString("months", "6")
            .Respond("application/json", SeriesJson);

        var result = await _client.GetSeriesAsync(TraineeId);

        result.Months.Should().Equal("2026-04", "2026-05", "2026-06");
        // arrays align index-wise with Months — FE renders as-is
        result.Consistency.Should().HaveCount(3);
        result.Consistency[0].Score.Should().Be(0.42);
        result.Consistency[1].DeltaPp.Should().Be(13);
        result.Weight[2].State.Should().Be(ResultsState.InsufficientData);
        result.Weight[2].LastKg.Should().BeNull(); // explicit null, never 0
        result.Strength.IndexPct[2].Value.Should().BeNull();
        result.Strength.ByType[0].Points[0].Delta.Should().Be(2.5);
        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetSeriesAsync_Passes_Months_Query_Param()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/series")
            .WithQueryString("months", "3")
            .Respond("application/json", SeriesJson);

        await _client.GetSeriesAsync(TraineeId, months: 3);

        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetSeriesAsync_Throws_Typed_On_5xx_With_Problem_Code()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/series")
            .Respond(HttpStatusCode.InternalServerError, "application/problem+json", """
                { "title": "Unexpected error", "status": 500, "code": "results_load_failed" }
                """);

        var ex = await Assert.ThrowsAsync<ResultsApiException>(
            () => _client.GetSeriesAsync(TraineeId));

        ex.Code.Should().Be("results_load_failed");
        ex.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetSeriesAsync_Throws_On_Incomplete_Body()
    {
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/series")
            .Respond("application/json", "null");

        var ex = await Assert.ThrowsAsync<ResultsApiException>(
            () => _client.GetSeriesAsync(TraineeId));

        ex.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Fixtures_And_Api_Client_Shape_Parity()
    {
        // The page must behave identically on fixtures vs the real client:
        // same DTO types end-to-end, no mapping layer anywhere.
        _mockHttp.When(HttpMethod.Get, "/Results/t-ana/month")
            .WithQueryString("month", "2026-09")
            .Respond("application/json", MonthJson);

        ResultsMonthResponse api = (await _client.GetMonthAsync(TraineeId, "2026-09"))!;
        IResultsClient fixtures = new ResultsFixtures();
        ResultsMonthResponse mock = (await fixtures.GetMonthAsync(TraineeId, "2026-09"))!;

        api.Should().BeOfType<ResultsMonthResponse>();
        mock.Consistency.State.Should().Be(api.Consistency.State);
        mock.Weight.LastKg.Should().Be(api.Weight.LastKg);
        _mockHttp.VerifyNoOutstandingExpectation();

        // JSON round-trip: fixtures serialize to the same contract the API sends.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(mock, options));
        doc.RootElement.GetProperty("consistency").GetProperty("activeSessions")
           .GetInt32().Should().Be(9);
    }
}
