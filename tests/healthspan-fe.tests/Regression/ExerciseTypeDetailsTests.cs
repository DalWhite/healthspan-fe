using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.ExerciseTypes;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using System.Net;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ExerciseTypeDetailsTests : BlazorTestBase
{
    [Fact]
    public void Organizes_Details_Into_Compact_Sections()
    {
        ConfigureWeightedExercise();

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 21));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
            cut.FindAll(".hs-exercise-detail-tab").Should().HaveCount(4);
            cut.Markup.Should().Contain("Mejor 1RM estimado");
            cut.Markup.Should().Contain("Chest");
        });

        cut.Find("button[aria-controls='exercise-section-history']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("4 series × 8 reps");
            cut.Markup.Should().Contain("80.0 kg");
            cut.Find("a.hs-exercise-history-row")
                .GetAttribute("href")
                .Should().StartWith("/training/history/");
        });

        cut.Find("button[aria-controls='exercise-section-variations']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Incline Bench Press");
            cut.Find("a[href^='/training/library/22?returnUrl=']")
                .Should().NotBeNull();
        });
    }

    [Fact]
    public void NoWeight_Exercise_Hides_Kilogram_And_OneRm_Metrics()
    {
        MockHttp.When("/ExerciseTypes/31")
            .Respond("application/json", """
                {
                  "id": 31,
                  "name": "Shoulder Mobility",
                  "description": "Controlled shoulder movement",
                  "weightType": 2,
                  "muscleAssignments": [],
                  "tags": [],
                  "variations": []
                }
            """);
        MockHttp.When("/Exercises?exerciseTypeId=31")
            .Respond("application/json", """
                [
                  {
                    "id": "31000000-0000-0000-0000-000000000001",
                    "series": 3,
                    "repetitions": 30,
                    "weight": 0,
                    "maxRepetition": 0,
                    "volume": 0,
                    "type": { "id": 31, "name": "Shoulder Mobility", "weightType": 2 },
                    "workout": {
                      "id": "31000000-0000-0000-0000-000000000010",
                      "date": "2026-07-20T00:00:00"
                    }
                  }
                ]
            """);
        MockHttp.When("/api/muscles")
            .Respond("application/json", "[]");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 31));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Solo repeticiones");
            cut.Markup.Should().NotContain("kg");
            cut.Markup.Should().NotContain("1RM estimado");
        });

        cut.Find("button[aria-controls='exercise-section-history']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("3 series × 30 reps");
            cut.Markup.Should().NotContain("kg");
            cut.Markup.Should().NotContain("1RM");
        });

        cut.Find("button[aria-controls='exercise-section-trends']").Click();
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain(
                "Las tendencias de carga y 1RM no se usan en este ejercicio."));
    }

    [Fact]
    public void Empty_History_Still_Uses_Catalog_Name_And_Clear_State()
    {
        MockHttp.When("/ExerciseTypes/41")
            .Respond("application/json", """
                {
                  "id": 41,
                  "name": "New Exercise",
                  "weightType": 0,
                  "muscleAssignments": [],
                  "tags": [],
                  "variations": []
                }
            """);
        MockHttp.When("/Exercises?exerciseTypeId=41")
            .Respond("application/json", "[]");
        MockHttp.When("/api/muscles")
            .Respond("application/json", "[]");
        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            "https://localhost/training/library/41?section=history");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 41));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("New Exercise");
            cut.Markup.Should().Contain("Sin historial todavía");
            cut.Markup.Should().Contain("Registrar entreno");
        });
    }

    [Fact]
    public void Back_Uses_Validated_Origin()
    {
        ConfigureWeightedExercise();
        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            "https://localhost/training/library/21?returnUrl=%2Ftraining%2Flibrary%3Fview%3Dfavorites");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 21));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
            cut.Find("button[aria-label='Volver']").Should().NotBeNull();
        });
        cut.Find("button[aria-label='Volver']").Click();

        cut.WaitForAssertion(() =>
            navigationManager.Uri.Should()
                .EndWith("/training/library?view=favorites"));
    }

    [Fact]
    public void Failed_Catalog_Request_Shows_Retryable_Error()
    {
        MockHttp.When("/ExerciseTypes/51")
            .Respond(HttpStatusCode.InternalServerError);
        MockHttp.When("/Exercises?exerciseTypeId=51")
            .Respond("application/json", "[]");
        MockHttp.When("/api/muscles")
            .Respond("application/json", "[]");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 51));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No se pudo cargar este ejercicio");
            cut.Markup.Should().Contain("Reintentar");
        });
    }

    [Fact]
    public void Training_Tag_Filters_History_And_Performance_Requests()
    {
        ConfigureWeightedExercise();
        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            "https://localhost/training/library/21?section=history");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 21));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bilbo");
            cut.Markup.Should().Contain("Tempo");
            cut.FindAll(".hs-exercise-history-row").Should().HaveCount(2);
        });

        cut.FindAll(".hs-analysis-tag-filter button")
            .Single(button => button.TextContent.Trim() == "Bilbo")
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-exercise-history-row").Should().ContainSingle();
            var row = cut.Find(".hs-exercise-history-row");
            row.TextContent.Should().Contain("Bilbo");
            row.TextContent.Should().NotContain("Tempo");
            navigationManager.Uri.Should().Contain("tag=Bilbo");
        });
    }

    [Fact]
    public void Trends_Use_One_Metric_Scale_At_A_Time()
    {
        ConfigureWeightedExercise();
        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            "https://localhost/training/library/21?section=trends");

        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(component => component.ExerciseTypeId, 21));

        cut.WaitForAssertion(() =>
        {
            cut.Find(".hs-exercise-trend-header")
                .TextContent.Should().Contain("1RM estimado");
            cut.FindAll(".hs-exercise-trend-metrics button.is-active")
                .Should().ContainSingle()
                .Which.TextContent.Trim().Should().Be("1RM");
            cut.Find(".hs-exercise-trend-latest")
                .TextContent.Should().Contain("101");
        }, timeout: TimeSpan.FromSeconds(10));

        cut.FindAll(".hs-exercise-trend-metrics button")
            .Single(button => button.TextContent.Trim() == "Volumen")
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".hs-exercise-trend-header")
                .TextContent.Should().Contain("Volumen por sesión");
            cut.Find(".hs-exercise-trend-header")
                .TextContent.Should().Contain(
                    "Carga total completada en cada entreno.");
            cut.FindAll(".hs-exercise-trend-metrics button.is-active")
                .Should().ContainSingle()
                .Which.TextContent.Trim().Should().Be("Volumen");
            cut.Find(".hs-exercise-trend-latest")
                .TextContent.Should().Contain("560 kg");
        }, timeout: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Hero_Owns_The_Favourite_Control_And_Uses_The_Focused_Endpoint()
    {
        ConfigureWeightedExercise();
        var cut = RenderComponent<ExerciseTypeDetails>(parameters => parameters
            .Add(p => p.ExerciseTypeId, 21));

        cut.WaitForElement(".hs-exercise-hero");

        // DESIGN.md §5.6: the detail hero carries the photo, the meta line and
        // the controls the library card deliberately dropped.
        cut.Find(".hs-exercise-hero-media img").GetAttribute("loading").Should().Be("lazy");
        cut.Find(".hs-exercise-hero-meta").TextContent.Should().Contain("barbell");
        cut.Find(".hs-exercise-hero-meta").TextContent.Should().Contain("1 variante");

        var button = cut.WaitForElement("button:contains('Añadir a favoritos')");

        MockHttp.Expect(HttpMethod.Put, "/ExerciseTypes/21/favourite")
            .Respond(HttpStatusCode.NoContent);

        button.Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("button:contains('Favorito')").Should().NotBeNull();
            MockHttp.VerifyNoOutstandingExpectation();
        });
    }

    private void ConfigureWeightedExercise()
    {
        MockHttp.When("/ExerciseTypes/21")
            .Respond("application/json", """
                {
                  "id": 21,
                  "name": "Bench Press",
                  "description": "Horizontal press",
                  "weightType": 0,
                  "muscleAssignments": [
                    { "muscleId": 1, "role": "Primary", "sortOrder": 1 }
                  ],
                  "tags": [
                    { "name": "barbell", "category": "equipment" }
                  ],
                  "variations": [
                    {
                      "id": 22,
                      "name": "Incline Bench Press",
                      "weightType": 0,
                      "muscleAssignments": [],
                      "tags": [
                        { "name": "incline", "category": "mechanic" }
                      ]
                    }
                  ]
                }
            """);
        MockHttp.When("/Exercises?exerciseTypeId=21")
            .Respond("application/json", """
                [
                  {
                    "id": "21000000-0000-0000-0000-000000000001",
                    "series": 4,
                    "repetitions": 8,
                    "weight": 80,
                    "maxRepetition": 101.3,
                    "volume": 2560,
                    "tags": ["Bilbo"],
                    "type": { "id": 21, "name": "Bench Press", "weightType": 0 },
                    "workout": {
                      "id": "21000000-0000-0000-0000-000000000010",
                      "date": "2026-07-20T00:00:00"
                    }
                  },
                  {
                    "id": "21000000-0000-0000-0000-000000000002",
                    "series": 3,
                    "repetitions": 10,
                    "weight": 65,
                    "maxRepetition": 86.6,
                    "volume": 1950,
                    "tags": ["Tempo"],
                    "type": { "id": 21, "name": "Bench Press", "weightType": 0 },
                    "workout": {
                      "id": "21000000-0000-0000-0000-000000000011",
                      "date": "2026-07-18T00:00:00"
                    }
                  }
                ]
            """);
        MockHttp.When("/Exercises/21/kpis")
            .Respond("application/json", """
                {
                  "maxRM": 101.3,
                  "maxRMDate": "2026-07-20T00:00:00",
                  "current1RM": 101.3,
                  "weeklyFrequency": 1
                }
            """);
        MockHttp.When("/Exercises/21/trends?type=RM")
            .Respond("application/json", """
                {
                  "points": [
                    { "data": 101.3, "time": "2026-07-20T00:00:00" }
                  ]
                }
            """);
        MockHttp.When("/Exercises/21/trends?type=Volume")
            .Respond("application/json", """
                {
                  "points": [
                    { "data": 2560, "time": "2026-07-20T00:00:00" }
                  ]
                }
            """);
        MockHttp.When("/Exercises/21/kpis?tag=Bilbo")
            .Respond("application/json", """
                {
                  "maxRM": 101.3,
                  "maxRMDate": "2026-07-20T00:00:00",
                  "current1RM": 101.3,
                  "weeklyFrequency": 1
                }
            """);
        MockHttp.When("/Exercises/21/trends?tag=Bilbo&type=RM")
            .Respond("application/json", """
                {
                  "points": [
                    { "data": 101.3, "time": "2026-07-20T00:00:00" }
                  ]
                }
            """);
        MockHttp.When("/Exercises/21/trends?tag=Bilbo&type=Volume")
            .Respond("application/json", """
                {
                  "points": [
                    { "data": 2560, "time": "2026-07-20T00:00:00" }
                  ]
                }
            """);
        MockHttp.When("/api/muscles")
            .Respond("application/json", """
                [
                  {
                    "id": 1,
                    "slug": "chest",
                    "name": "Chest",
                    "side": "front",
                    "muscleGroup": "Chest",
                    "sortOrder": 10
                  }
                ]
            """);
    }
}
