using Bunit;
using Bunit.TestDoubles;
using AngleSharp.Dom;
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
    public void CreateExerciseType_NameBlur_EmptyName_ShowsInlineError()
    {
        ConfigureCatalogResponses();

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Blur();

        renderer.WaitForAssertion(() =>
        {
            renderer.Markup.Should().Contain("Name is required.");
            renderer.Markup.Should().Contain("mud-input-error");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateExerciseType_NameBlur_TooShortName_ShowsSpecificInlineError()
    {
        ConfigureCatalogResponses();

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Change("X");
        nameInput.Blur();

        renderer.WaitForAssertion(() =>
        {
            renderer.Markup.Should().Contain("Name must be at least 2 characters.");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateExerciseType_NameBlur_DuplicateName_ShowsInlineAlreadyExists()
    {
        ConfigureCatalogResponses();

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Change("sit up");
        nameInput.Blur();

        // Debounced uniqueness check must surface the specific error next to the field.
        renderer.WaitForAssertion(() =>
        {
            renderer.Markup.Should().Contain("'sit up' already exists. Pick a different name.");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateExerciseType_NameBlur_UniqueName_ShowsSuccessIndicator()
    {
        ConfigureCatalogResponses();

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Change("Burpee");
        nameInput.Blur();

        renderer.WaitForAssertion(() =>
        {
            renderer.FindAll(".mud-input-helper-text.mud-input-error").Should().BeEmpty();
            renderer.FindAll(".mud-input-adornment svg").Should().NotBeEmpty();
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateExerciseType_FailedSubmit_PreservesUserInput()
    {
        ConfigureCatalogResponses();

        MockHttp.Expect(HttpMethod.Post, "/ExerciseTypes")
            .Respond(HttpStatusCode.Conflict, "application/problem+json",
                """
                { "title": "An exercise type named 'Burpee' already exists.", "status": 409 }
                """);

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Change("Burpee");
        nameInput.Blur();

        // Drain the debounced inline uniqueness check before submitting: the name is
        // unique in the catalog so the client-side check passes, and the inline GET is
        // not left racing the submit POST on the shared MockHttp handler (flaky deadlock).
        renderer.WaitForAssertion(() =>
        {
            renderer.FindAll(".mud-input-helper-text.mud-input-error").Should().BeEmpty();
            renderer.FindAll(".mud-input-adornment svg").Should().NotBeEmpty();
        }, timeout: TimeSpan.FromSeconds(10));

        SelectPrimaryMuscle(renderer, "Abs");
        ClickSaveForm(renderer);

        renderer.WaitForAssertion(() =>
        {
            MockHttp.VerifyNoOutstandingExpectation();
            renderer.Markup.Should().Contain("already exists");
        }, timeout: TimeSpan.FromSeconds(45));

        // NN/g: never reset the form after a failed submit.
        renderer.Find("input.mud-input-slot").GetAttribute("value").Should().Be("Burpee");
    }

    [Fact]
    public void CreateExerciseType_Shows_Backend_ValidationError()
    {
        ConfigureCatalogResponses();

        MockHttp.Expect(HttpMethod.Post, "/ExerciseTypes")
            .Respond(HttpStatusCode.BadRequest, "application/problem+json",
                """
                { "title": "An exercise type named 'Burpee' already exists.", "status": 400 }
                """);

        var renderer = RenderCreatePage();

        var nameInput = renderer.WaitForElement("input.mud-input-slot", timeout: TimeSpan.FromSeconds(10));
        nameInput.Change("Burpee");
        nameInput.Blur();

        // The name is unique in the catalog, so the inline check passes and the submit
        // reaches the backend. Draining the debounced inline GET first keeps it from
        // racing the submit POST on the shared MockHttp handler (flaky deadlock).
        renderer.WaitForAssertion(() =>
        {
            renderer.FindAll(".mud-input-helper-text.mud-input-error").Should().BeEmpty();
            renderer.FindAll(".mud-input-adornment svg").Should().NotBeEmpty();
        }, timeout: TimeSpan.FromSeconds(10));

        SelectPrimaryMuscle(renderer, "Abs");
        ClickSaveForm(renderer);

        // WaitForAssertion polls on a timer that can be starved under full-suite load on
        // low-power CI boxes; the generous timeout plus the popover-close/drain steps
        // above keep the submit chain from deadlocking.
        renderer.WaitForAssertion(() =>
        {
            renderer.Markup.Should().Contain("already exists");
            MockHttp.VerifyNoOutstandingExpectation();
        }, timeout: TimeSpan.FromSeconds(45));
    }

    private IRenderedFragment RenderCreatePage()
        => Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.CreateExerciseTypes>(1);
            builder.CloseComponent();
        });

    private static void SelectPrimaryMuscle(IRenderedFragment renderer, string muscleName)
    {
        IElement primarySelect = null;
        renderer.WaitForAssertion(() =>
        {
            var selects = renderer.FindAll("div.mud-input-control.mud-select");
            selects.Count.Should().BeGreaterThan(1);
            primarySelect = selects[1];
        }, timeout: TimeSpan.FromSeconds(10));
        primarySelect.Click();

        // Click the option and confirm the selection actually registered (the popover
        // re-render can swallow a single click on slow CI boxes). Retry until selected.
        renderer.WaitForAssertion(() =>
        {
            var option = renderer.FindAll(".mud-list-item")
                .First(item => item.TextContent.Trim() == muscleName);
            if (!option.ClassList.Contains("mud-selected-item"))
            {
                option.Click();
            }
            option = renderer.FindAll(".mud-list-item")
                .First(item => item.TextContent.Trim() == muscleName);
            option.ClassList.Contains("mud-selected-item").Should().BeTrue();
        }, timeout: TimeSpan.FromSeconds(15));

        // Close the multi-select popover and confirm it is gone before submitting.
        // An open popover makes the Save click race MudSelect's focus-out re-render,
        // which intermittently deadlocks the submit chain on the bUnit sync context.
        primarySelect.Click();
        renderer.WaitForAssertion(
            () => renderer.FindAll(".mud-list-item").Should().BeEmpty(),
            timeout: TimeSpan.FromSeconds(10));
    }

    private static void ClickSaveForm(IRenderedFragment renderer)
    {
        IElement saveButton = null;
        renderer.WaitForAssertion(() =>
        {
            saveButton = renderer.FindAll("button")
                .First(button => button.TextContent.Contains("Save Form"));
        }, timeout: TimeSpan.FromSeconds(10));
        saveButton.Click();
    }

    private void ConfigureCatalogResponses()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
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
