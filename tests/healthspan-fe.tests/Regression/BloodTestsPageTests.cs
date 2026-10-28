using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class BloodTestsPageTests : BlazorTestBase
{
    [Fact]
    public void Renders_Loading_Then_BloodTests_Content()
    {
        MockHttp.When("/BloodTests")
            .Respond("application/json", """
                [
                    {
                        "id": "00000000-0000-0000-0000-000000000001",
                        "testDate": "2025-06-01T00:00:00",
                        "labName": "Test Lab",
                        "processingStatus": 1,
                        "requiresReview": false,
                        "markerCount": 1
                    }
                ]
            """);

        var cut = RenderComponent<BloodTests>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Test Lab");
        });
    }

    [Fact]
    public void Renders_EmptyState_When_No_BloodTests()
    {
        MockHttp.When("/BloodTests")
            .Respond("application/json", "[]");

        var cut = RenderComponent<BloodTests>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("analítica");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Renders_PendingReview_Badge_When_Present()
    {
        MockHttp.When("/BloodTests")
            .Respond("application/json", """
                [
                    {
                        "id": "00000000-0000-0000-0000-000000000002",
                        "testDate": "2025-06-02T00:00:00",
                        "labName": "Pending Lab",
                        "processingStatus": 0,
                        "requiresReview": true,
                        "markerCount": 0
                    }
                ]
            """);

        var cut = RenderComponent<BloodTests>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Pendiente de revisión");
            cut.Markup.Should().Contain("Revisar resultados");
        });
    }
}
