using Bunit;
using AngleSharp.Dom;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using healthspan_fe.tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Exercise Library load-size guard (t_f884c566).
///
/// QA finding: with the real catalog (876 exercise types -> 715 main cards, each a MudCard
/// with a MudMenu and a remote background image) the WASM main thread froze for ~30s on the
/// first render. The grid now renders one page of 40 cards and lazy-loads card images.
/// These tests pin the DOM size, the paging behaviour and the search/filter contract.
/// </summary>
public class ExerciseLibraryPaginationTests : BlazorTestBase
{
    private const int MainCount = 715;
    private const int VariationCount = 161;
    private const int PageSize = 40;

    [Fact]
    public void LargeLibrary_InitialRender_DrawsOnlyOnePage_WithinTwoSeconds()
    {
        ConfigureLargeCatalog();

        var stopwatch = Stopwatch.StartNew();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));
        stopwatch.Stop();

        // The whole catalog is still known to the user: the count names all 715 matches.
        cut.Markup.Should().Contain($"Showing 1\u2013{PageSize} of {MainCount} exercises");

        // Acceptance: initial render must not freeze the page. In the WASM build the same
        // render pass took ~30s before this change (measured in the browser).
        stopwatch.Elapsed.TotalSeconds.Should().BeLessThan(2);
    }

    [Fact]
    public void LargeLibrary_CardImages_AreLazyLoaded_NotEagerBackgrounds()
    {
        ConfigureLargeCatalog();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));

        var photos = cut.FindAll(".hs-et-card-photo img");
        photos.Should().HaveCount(PageSize);
        photos.All(photo => photo.GetAttribute("loading") == "lazy").Should().BeTrue();
        photos.All(photo => photo.GetAttribute("decoding") == "async").Should().BeTrue();
        // The photo is decorative: the accessible name lives in the card's aria-label
        // and title, so alt="" and a name-less placeholder card renders no <img> at all.
        photos.All(photo => photo.GetAttribute("alt") == "").Should().BeTrue();

        // The old eager CSS background-image per card is gone (that is what fired 873
        // remote image requests at once).
        cut.FindAll(".hs-et-card-photo[style]").Should().BeEmpty();
    }

    [Fact]
    public void Paging_NextPage_Replaces_The_Rendered_Slice()
    {
        ConfigureLargeCatalog();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));

        ClickPage(cut, "2");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".exercise-card").Should().HaveCount(PageSize);
            cut.Markup.Should().Contain($"Showing {PageSize + 1}\u2013{PageSize * 2} of {MainCount} exercises");
            cut.Find(".hs-et-card-title").TextContent.Trim().Should().Be("Exercise 041");
        }, timeout: TimeSpan.FromSeconds(10));

        // Still one page of DOM, not the full catalog.
        cut.FindAll(".exercise-card").Should().HaveCount(PageSize);
    }

    [Fact]
    public void Paging_State_Is_Restored_When_Returning_To_The_Library()
    {
        ConfigureLargeCatalog();
        var appState = Services.GetRequiredService<AppStateService>();

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));
        ClickPage(cut, "3");
        cut.WaitForAssertion(
            () => appState.ExerciseLibraryView.Page.Should().Be(3),
            timeout: TimeSpan.FromSeconds(10));
        cut.Dispose();

        // Landing back from an exercise detail page must return to the page the user was on,
        // not to the top of the catalog.
        var restored = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        restored.WaitForAssertion(() =>
        {
            restored.Find(".hs-et-card-title").TextContent.Trim().Should().Be("Exercise 081");
            restored.Markup.Should().Contain($"Showing 81\u2013120 of {MainCount} exercises");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Search_Resets_To_First_Page_And_Filters_The_Full_Catalog()
    {
        ConfigureLargeCatalog();
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));
        ClickPage(cut, "4");

        var search = cut.WaitForElement(
            "input[placeholder='Search exercises, variations, or tags...']");
        search.Input("Exercise 04");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".exercise-card").Should().HaveCount(10);
            cut.Find(".hs-et-card-title").TextContent.Trim().Should().Be("Exercise 040");
            cut.Markup.Should().Contain("10 exercises");
            cut.FindAll(".mud-pagination").Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Tag_Filter_Still_Runs_Over_The_Full_Catalog_And_Pages_The_Result()
    {
        ConfigureLargeCatalog();
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(PageSize),
            timeout: TimeSpan.FromSeconds(30));

        cut.Find(".hs-et-filter-toggle").Click();
        cut.WaitForAssertion(
            () => cut.FindAll("div.mud-input-control.mud-select").Should().HaveCount(3),
            timeout: TimeSpan.FromSeconds(10));

        // Equipment "band" is on every even-numbered exercise: 357 of the 715 main cards.
        SelectEquipment(cut, "band");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".exercise-card").Should().HaveCount(PageSize);
            cut.Markup.Should().Contain($"Showing 1\u2013{PageSize} of 357 exercises");
            cut.Find(".hs-et-card-title").TextContent.Trim().Should().Be("Exercise 002");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void SmallCatalog_Does_Not_Show_Pagination()
    {
        ConfigureCatalogResponses(
            """
            [
                {
                  "id": 1,
                  "name": "Sit Up",
                  "description": "Base sit-up pattern",
                  "muscleAssignments": [ { "muscleId": 11, "role": "Primary", "sortOrder": 1 } ],
                  "isFavourite": true,
                  "parentExerciseTypeId": null,
                  "tags": [ { "name": "bodyweight", "category": "equipment" } ]
                }
            ]
            """);

        var cut = RenderComponent<healthspan_fe_wasm_pwa.Pages.ExerciseTypes.ExerciseTypes>();
        cut.WaitForAssertion(
            () => cut.FindAll(".exercise-card").Should().HaveCount(1),
            timeout: TimeSpan.FromSeconds(10));

        cut.FindAll(".mud-pagination").Should().BeEmpty();
        cut.Markup.Should().Contain("1 exercise");
    }

    private void ClickPage(IRenderedFragment renderer, string pageNumber)
    {
        renderer.WaitForElements($"button[aria-label='Page {pageNumber}']", TimeSpan.FromSeconds(10));
        renderer.Find($"button[aria-label='Page {pageNumber}']").Click();
    }

    private void SelectEquipment(IRenderedFragment renderer, string option)
    {
        renderer.WaitForElements("div.mud-input-control.mud-select", TimeSpan.FromSeconds(10));
        var equipmentSelect = renderer.FindAll("div.mud-input-control.mud-select")[1];
        equipmentSelect.Click();

        renderer.WaitForAssertion(
            () => renderer.FindAll(".mud-list-item").Should().NotBeEmpty(),
            timeout: TimeSpan.FromSeconds(15));
        renderer.FindAll(".mud-list-item")
            .First(item => item.TextContent.Trim() == option).Click();
    }

    private void ConfigureLargeCatalog()
        => ConfigureCatalogResponses(BuildLargeCatalogJson());

    private void ConfigureCatalogResponses(string exerciseTypesJson)
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", exerciseTypesJson);
        MockHttp.When("/api/muscles")
            .Respond("application/json", """
                [
                    { "id": 11, "slug": "abs", "name": "Abs", "side": "front", "muscleGroup": "Core", "sortOrder": 110 },
                    { "id": 14, "slug": "hip-flexors", "name": "Hip flexors", "side": "front", "muscleGroup": "Legs", "sortOrder": 140 }
                ]
            """);
    }

    /// <summary>Mirrors the seeded dev catalog: 876 types, 161 of them variants.</summary>
    private static string BuildLargeCatalogJson()
    {
        var items = new List<object>(MainCount + VariationCount);

        for (var i = 1; i <= MainCount; i++)
        {
            items.Add(new
            {
                id = i,
                name = $"Exercise {i:D3}",
                description = $"Generated item {i:D3}",
                imageRoute = $"https://cdn.example.test/exercises/{i}.jpg",
                weightType = "Standard",
                isFavourite = i % 50 == 0,
                parentExerciseTypeId = (int?)null,
                tags = new object[]
                {
                    new { name = i % 2 == 0 ? "band" : "barbell", category = "equipment" },
                    new { name = "compound", category = "mechanic" },
                    new { name = "strength", category = "type" }
                },
                muscleAssignments = new object[]
                {
                    new { muscleId = 11, role = "Primary", sortOrder = 1 }
                }
            });
        }

        for (var i = 1; i <= VariationCount; i++)
        {
            items.Add(new
            {
                id = MainCount + i,
                name = $"Variation {i:D3}",
                description = $"Generated variant {i:D3}",
                imageRoute = $"https://cdn.example.test/exercises/variant-{i}.jpg",
                weightType = "Standard",
                isFavourite = false,
                parentExerciseTypeId = ((i - 1) % MainCount) + 1,
                tags = new object[]
                {
                    new { name = "bodyweight", category = "equipment" },
                    new { name = "variation", category = "type" }
                },
                muscleAssignments = new object[]
                {
                    new { muscleId = 14, role = "Primary", sortOrder = 1 }
                }
            });
        }

        return JsonSerializer.Serialize(items);
    }
}
