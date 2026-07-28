using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using RichardSzalay.MockHttp;
using Xunit;
using HealthProfilePage =
    healthspan_fe_wasm_pwa.Pages.Health.HealthProfile;

namespace healthspan_fe.tests.Regression;

public sealed class HealthProfilePageTests : BlazorTestBase
{
    [Fact]
    public void Renders_Saved_Health_Profile()
    {
        MockHttp.When("/HealthProfile")
            .Respond("application/json", """
                {
                  "name": "David",
                  "dateOfBirth": "1989-06-15",
                  "biologicalSex": 1,
                  "heightCm": 180,
                  "weightKg": 80
                }
                """);

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Personal profile");
            cut.Markup.Should().Contain("David");
            cut.Markup.Should().Contain("Current weight");
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Save"));
        });
    }

    [Fact]
    public void Renders_Load_Error_Without_Hiding_Form()
    {
        MockHttp.When("/HealthProfile")
            .Throw(new HttpRequestException("offline"));

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain(
                "Profile could not be loaded");
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Save"));
        });
    }
}
