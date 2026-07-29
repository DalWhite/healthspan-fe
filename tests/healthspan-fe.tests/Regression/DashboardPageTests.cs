using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class DashboardPageTests : BlazorTestBase
{
    [Fact]
    public void Renders_Loading_Then_Dashboard_Content()
    {
        MockHttp.When("/Dashboard/summary")
            .Respond("application/json", """
                {
                    "recentWorkoutCount": 5,
                    "bloodTestCount": 2,
                    "latestBloodTestDate": "2025-06-01T00:00:00",
                    "outOfRangeMarkersInLatestBloodTest": 1,
                    "latestAssessmentDate": "2025-05-15T00:00:00",
                    "latestAssessmentAverageScore": 7.5
                }
            """);
        MockHttp.When("/BloodTests")
            .Respond("application/json", """
                [
                    {
                        "id": "00000000-0000-0000-0000-000000000001",
                        "testDate": "2025-06-01T00:00:00",
                        "labName": "Test Lab",
                        "processingStatus": 1,
                        "markerCount": 3
                    }
                ]
            """);
        MockHttp.When("/BloodTests/markers/latest")
            .Respond("application/json", "[]");
        MockHttp.When("/HealthProfile")
            .Respond("application/json", """
                {
                    "dateOfBirth": "1990-01-01",
                    "biologicalSex": 1
                }
            """);

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Latest blood test");
            cut.Markup.Should().Contain("3 markers");
        });
    }

    [Fact]
    public void Renders_Error_State_On_Failure()
    {
        MockHttp.When("/Dashboard/summary")
            .Respond(System.Net.HttpStatusCode.InternalServerError);
        MockHttp.When("/BloodTests")
            .Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest")
            .Respond("application/json", "[]");
        MockHttp.When("/HealthProfile")
            .Respond("application/json", "{}");

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain(
                "Health information could not be loaded");
        });
    }

    [Fact]
    public void Renders_Useful_Actions_With_Zero_Values()
    {
        MockHttp.When("/Dashboard/summary")
            .Respond("application/json", """
                {
                    "recentWorkoutCount": 0,
                    "bloodTestCount": 0,
                    "latestBloodTestDate": null,
                    "outOfRangeMarkersInLatestBloodTest": 0,
                    "latestAssessmentDate": null,
                    "latestAssessmentAverageScore": null
                }
            """);
        MockHttp.When("/BloodTests")
            .Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest")
            .Respond("application/json", "[]");
        MockHttp.When("/HealthProfile")
            .Respond("application/json", """
                {
                    "dateOfBirth": "1990-01-01",
                    "biologicalSex": 1
                }
            """);

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Add first report");
            cut.Markup.Should().Contain("Record baseline");
        });
    }
}
