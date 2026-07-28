using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public sealed class AssessmentsPageTests : BlazorTestBase
{
    [Fact]
    public void Empty_History_Does_Not_Request_Latest_Separately()
    {
        MockHttp.When("/GalpinAssessments/catalog")
            .Respond("application/json", "[]");
        MockHttp.When("/GalpinAssessments")
            .Respond("application/json", "[]");
        var latestRequest = MockHttp
            .When("/GalpinAssessments/latest")
            .Respond("application/json", "null");

        var cut = RenderComponent<Assessments>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No assessments yet");
            cut.Markup.Should().NotContain(
                "Assessments could not be loaded");
            MockHttp.GetMatchCount(latestRequest).Should().Be(0);
        });
    }
}
