using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using System.Linq;
using System.Net;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Pages.ExerciseTypes;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ExerciseTypesPageTests : BlazorTestBase
{
    [Fact]
    public void Renders_ExerciseTypes_Page()
    {
        ConfigureCatalogResponses();

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Sit Up");
            cut.Markup.Should().Contain("Romanian Sit Up");
            cut.Markup.Should().Contain("bodyweight");
            cut.FindAll(".exercise-card").Should().HaveCount(1);
            cut.Find(".exercise-card").GetAttribute("role").Should().Be("link");
            cut.Find(".exercise-card").GetAttribute("tabindex").Should().Be("0");
            cut.Markup.Should().Contain("1 exercise");
            cut.Markup.Should().Contain("Filters");
            cut.FindAll("button[aria-label='View stats']").Should().HaveCount(1);
            cut.FindAll("button[aria-label='Create exercise for today']").Should().BeEmpty();
            cut.FindAll(".hs-et-card-menu").Should().HaveCount(1);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Search_WithNoMatch_ShowsFilterAwareEmptyState()
    {
        ConfigureCatalogResponses();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();

        var search = cut.WaitForElement("input[placeholder='Search exercises, variations, or tags...']");
        search.Input("not-in-this-catalog");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No matching exercises");
            cut.Markup.Should().Contain("Clear filters");
            cut.FindAll(".exercise-card").Should().BeEmpty();
        });
    }

    [Fact]
    public void Favourite_Button_UsesFocusedEndpoint()
    {
        ConfigureCatalogResponses();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        var favoriteButton = cut.WaitForElement("button[aria-label='Remove from favorites']");

        MockHttp.Expect(HttpMethod.Put, "/ExerciseTypes/1/favourite")
            .Respond(HttpStatusCode.NoContent);

        favoriteButton.Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button[aria-label='Add to favorites']").Should().HaveCount(1);
            MockHttp.VerifyNoOutstandingExpectation();
        });
    }

    [Fact]
    public void Library_Preserves_Search_State_And_Passes_ReturnRoute()
    {
        ConfigureCatalogResponses();
        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/training/library");

        var cut = RenderComponent<
            healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        var search = cut.WaitForElement(
            "input[placeholder='Search exercises, variations, or tags...']");
        var appState = Services.GetRequiredService<AppStateService>();
        search.Input("Romanian");

        cut.WaitForAssertion(() =>
        {
            appState.ExerciseLibraryView.Search.Should().Be("Romanian");
            cut.FindAll(".exercise-card").Should().ContainSingle();
        });
        cut.Find(".exercise-card").Click();

        cut.WaitForAssertion(() =>
            navigationManager.Uri.Should().Contain(
                "/training/library/1?returnUrl=%2Ftraining%2Flibrary"));

        cut.Dispose();
        navigationManager.NavigateTo("https://localhost/training/library");
        var restored = RenderComponent<
            healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();

        restored.WaitForAssertion(() =>
            restored.Find(
                    "input[placeholder='Search exercises, variations, or tags...']")
                .GetAttribute("value")
                .Should().Be("Romanian"));
    }

    [Fact]
    public void TrainedExerciseTypes_Page_Requests_TrainedEndpoint_And_Renders_Only_Trained()
    {
        ConfigureCatalogResponses();
        MockHttp.When("/ExerciseTypes/trained")
            .Respond("application/json", """
                [
                    {
                      "id": 1,
                      "name": "Sit Up",
                      "description": "Base sit-up pattern",
                      "muscleAssignments": [
                        { "muscleId": 11, "role": "Primary", "sortOrder": 1 }
                      ],
                      "isFavourite": false,
                      "parentExerciseTypeId": null,
                      "tags": [
                        { "name": "bodyweight", "category": "equipment" }
                      ]
                    }
                ]
            """);

        var cut = RenderComponent<TrainedExerciseTypes>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Sit Up");
            cut.Markup.Should().NotContain("Romanian Sit Up");
            cut.FindAll(".exercise-card").Should().HaveCount(1);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CreateExerciseType_Shows_Backend_ValidationError()
    {
        ConfigureCatalogResponses();

        MockHttp.Expect(HttpMethod.Post, "/ExerciseTypes")
            .Respond(HttpStatusCode.BadRequest, "application/problem+json",
                """
                { "title": "An exercise type named 'Sit Up' already exists.", "status": 400 }
                """);

        var renderer = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.CreateExerciseTypes>(1);
            builder.CloseComponent();
        });

        var nameInput = renderer.WaitForElement("input.mud-input-root");
        nameInput.Input("Sit Up");

        var primarySelect = renderer.FindAll("div.mud-input-control.mud-select")[1];
        primarySelect.Click();

        renderer.WaitForAssertion(() =>
        {
            var absOption = renderer.FindAll(".mud-list-item")
                .First(item => item.TextContent.Trim() == "Abs");
            absOption.Click();
        }, timeout: TimeSpan.FromSeconds(3));

        renderer.FindAll("button").First(button => button.TextContent.Contains("Save Form")).Click();

        renderer.WaitForAssertion(() =>
        {
            renderer.Markup.Should().Contain("already exists");
            MockHttp.VerifyNoOutstandingExpectation();
        }, timeout: TimeSpan.FromSeconds(3));
    }

    private void ConfigureCatalogResponses()
    {
        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    {
                      "id": 1,
                      "name": "Sit Up",
                      "description": "Base sit-up pattern",
                      "muscleAssignments": [
                        { "muscleId": 11, "role": "Primary", "sortOrder": 1 },
                        { "muscleId": 14, "role": "Secondary", "sortOrder": 1 }
                      ],
                      "isFavourite": true,
                      "parentExerciseTypeId": null,
                      "tags": [
                        { "name": "bodyweight", "category": "equipment" },
                        { "name": "rotation", "category": "mechanic" },
                        { "name": "core", "category": "type" }
                      ]
                    },
                    {
                      "id": 2,
                      "name": "Romanian Sit Up",
                      "description": "Variation with different emphasis",
                      "muscleAssignments": [
                        { "muscleId": 14, "role": "Primary", "sortOrder": 1 },
                        { "muscleId": 11, "role": "Secondary", "sortOrder": 1 }
                      ],
                      "isFavourite": false,
                      "parentExerciseTypeId": 1,
                      "tags": [
                        { "name": "bodyweight", "category": "equipment" },
                        { "name": "variation", "category": "type" }
                      ]
                    }
                ]
            """);
        MockHttp.When("/api/muscles")
            .Respond("application/json", """
                [
                    { "id": 11, "slug": "abs", "name": "Abs", "side": "front", "muscleGroup": "Core", "sortOrder": 110 },
                    { "id": 14, "slug": "hip-flexors", "name": "Hip flexors", "side": "front", "muscleGroup": "Legs", "sortOrder": 140 }
                ]
            """);
    }
}
