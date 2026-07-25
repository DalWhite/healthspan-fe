using System.Net;
using System.Net.Http.Json;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class WorkoutsPageTests : BlazorTestBase
{
    [Fact]
    public void Renders_Loading_Then_Workouts_Table()
    {
        // Arrange — mock returns a short workout list
        MockHttp.When("/workouts")
            .Respond("application/json", """
                [
                    { "id": "00000000-0000-0000-0000-000000000001", "date": "2025-06-01T00:00:00", "type": "Push" },
                    { "id": "00000000-0000-0000-0000-000000000002", "date": "2025-06-02T00:00:00", "type": "Pull" }
                ]
            """);

        // Act
        var cut = RenderComponent<Workouts>();

        // Assert — wait for async data load to complete
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Push");
            cut.Markup.Should().Contain("Pull");
        });
    }

    [Fact]
    public void Renders_EmptyState_When_No_Workouts()
    {
        MockHttp.When("/workouts")
            .Respond("application/json", "[]");

        var cut = RenderComponent<Workouts>();

        cut.Markup.Should().Contain("No workouts yet");
    }
}
