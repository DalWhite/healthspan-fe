using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using RichardSzalay.MockHttp;
using System.Linq;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Exercise photo fallback (t_8ffed894, decision t_b67627aa).
///
/// The URL comes ONLY from the record's ImageRoute. Without one no URL is built —
/// the card keeps its CSS placeholder — so a card can never fire a 404 request to
/// the derived `ExerciseImages/{Name}.jpg` path that no asset matches.
/// </summary>
public class ExercisePhotoFallbackTests : BlazorTestBase
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PhotoUrl_NeverDerivesAPathFromTheName(string? imageRoute)
    {
        ExercisePhoto.Url(imageRoute).Should().BeNull();
    }

    [Fact]
    public void PhotoUrl_ServesImageRouteVerbatim()
    {
        ExercisePhoto.Url("https://cdn.example.test/a.jpg")
            .Should().Be("https://cdn.example.test/a.jpg");
    }

    [Fact]
    public void LibraryCard_WithoutImageRoute_RendersPlaceholder_AndNoRequest()
    {
        ConfigureCatalog(
            """
            [
                {
                  "id": 1,
                  "name": "Kettlebell Halo",
                  "muscleAssignments": [],
                  "tags": [],
                  "variations": []
                }
            ]
            """);

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".hs-et-card-photo").Should().HaveCount(1),
            timeout: System.TimeSpan.FromSeconds(10));

        // Placeholder only: no <img>, therefore no request and no possible 404.
        cut.FindAll(".hs-et-card-photo img").Should().BeEmpty();
        cut.Markup.Should().NotContain("ExerciseImages/");

        // The name is still accessible through the card, not through a broken image.
        cut.Find(".hs-et-card-title").TextContent.Trim().Should().Be("Kettlebell Halo");
        cut.Find("div[role='link']").GetAttribute("aria-label")
            .Should().Contain("Kettlebell Halo");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public void LibraryCard_WithImageRoute_RendersDecorativeImg_WithoutNameAlt()
    {
        ConfigureCatalog(
            """
            [
                {
                  "id": 2,
                  "name": "Bench Press",
                  "imageRoute": "https://cdn.example.test/bench.jpg",
                  "muscleAssignments": [],
                  "tags": [],
                  "variations": []
                }
            ]
            """);

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".hs-et-card-photo img").Should().HaveCount(1),
            timeout: System.TimeSpan.FromSeconds(10));

        var img = cut.Find(".hs-et-card-photo img");
        img.GetAttribute("src").Should().Be("https://cdn.example.test/bench.jpg");
        img.GetAttribute("alt").Should().BeEmpty();
        img.GetAttribute("loading").Should().Be("lazy");
        img.GetAttribute("onerror").Should().Be("this.remove()");
    }

    [Fact]
    public void ExerciseDetail_Hero_WithoutImageRoute_RendersPlaceholder_AndNoRequest()
    {
        MockHttp.When("/ExerciseTypes/31")
            .Respond("application/json", """
                {
                  "id": 31,
                  "name": "Shoulder Mobility",
                  "weightType": 2,
                  "muscleAssignments": [],
                  "tags": [],
                  "variations": []
                }
            """);
        MockHttp.When("/Exercises?exerciseTypeId=31").Respond("application/json", "[]");
        MockHttp.When("/api/muscles").Respond("application/json", "[]");

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypeDetails>(
            parameters => parameters.Add(p => p.ExerciseTypeId, 31));

        cut.WaitForElement(".hs-exercise-hero-media");
        cut.FindAll(".hs-exercise-hero-media img").Should().BeEmpty();
        cut.Markup.Should().NotContain("ExerciseImages/");
        MockHttp.VerifyNoOutstandingExpectation();
    }

    private void ConfigureCatalog(string exerciseTypesJson)
    {
        MockHttp.When("/ExerciseTypes").Respond("application/json", exerciseTypesJson);
        MockHttp.When("/api/muscles").Respond("application/json", "[]");
    }
}
