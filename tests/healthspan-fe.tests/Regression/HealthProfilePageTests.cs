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
                  "dateOfBirth": "1989-06-15",
                  "biologicalSex": 1
                }
                """);

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Health profile");
            cut.Markup.Should().Contain("Complete");
            cut.Markup.Should().Contain("Save health profile");
            cut.Markup.Should().Contain(
                "never changes your original lab result");
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
                "Health profile could not be loaded");
            cut.Markup.Should().Contain("Save health profile");
        });
    }
}
