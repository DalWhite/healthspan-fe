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
            cut.Markup.Should().Contain("Best estimated 1RM");
            cut.Markup.Should().Contain("Chest");
        });

        cut.Find("button[aria-controls='exercise-section-history']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("4 sets × 8 reps");
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
            cut.Markup.Should().Contain("Repetitions only");
            cut.Markup.Should().NotContain("kg");
            cut.Markup.Should().NotContain("estimated 1RM");
        });

        cut.Find("button[aria-controls='exercise-section-history']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("3 sets × 30 reps");
            cut.Markup.Should().NotContain("kg");
            cut.Markup.Should().NotContain("1RM");
        });

        cut.Find("button[aria-controls='exercise-section-trends']").Click();
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain(
                "Load and 1RM trends aren't used for this exercise."));
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
            cut.Markup.Should().Contain("No history yet");
            cut.Markup.Should().Contain("Open Today");
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
            cut.Find("button[aria-label='Back']").Should().NotBeNull();
        });
        cut.Find("button[aria-label='Back']").Click();

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
            cut.Markup.Should().Contain("Couldn't load this exercise");
            cut.Markup.Should().Contain("Retry");
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
