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
                        "markers": [{ "name": "Glucose", "value": 90, "unit": "mg/dL", "status": 1 }]
                    }
                ]
            """);
        MockHttp.When("/BloodTests/pending-review")
            .Respond("application/json", "[]");
        MockHttp.When("/BloodTests/*")
            .Respond("application/json", """
                {
                    "id": "00000000-0000-0000-0000-000000000001",
                    "testDate": "2025-06-01T00:00:00",
                    "labName": "Test Lab",
                    "processingStatus": 1,
                    "requiresReview": false,
                    "markers": [{ "name": "Glucose", "value": 90, "unit": "mg/dL", "status": 1 }]
                }
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
        MockHttp.When("/BloodTests/pending-review")
            .Respond("application/json", "[]");

        var cut = RenderComponent<BloodTests>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("blood test");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Renders_PendingReview_Badge_When_Present()
    {
        MockHttp.When("/BloodTests")
            .Respond("application/json", "[]");
        MockHttp.When("/BloodTests/pending-review")
            .Respond("application/json", """
                [
                    {
                        "id": "00000000-0000-0000-0000-000000000002",
                        "testDate": "2025-06-02T00:00:00",
                        "labName": "Pending Lab",
                        "processingStatus": 0,
                        "requiresReview": true,
                        "markers": []
                    }
                ]
            """);
        MockHttp.When("/BloodTests/*")
            .Respond("application/json", """
                {
                    "id": "00000000-0000-0000-0000-000000000002",
                    "testDate": "2025-06-02T00:00:00",
                    "labName": "Pending Lab",
                    "processingStatus": 0,
                    "requiresReview": true,
                    "markers": []
                }
            """);

        var cut = RenderComponent<BloodTests>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Pending");
        });
    }
}
