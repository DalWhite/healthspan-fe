using System.Net;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class WorkoutsPageTests : BlazorTestBase
{
    private static readonly Guid PushWorkoutId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid PullWorkoutId =
        Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public void Renders_Workout_Summary_Cards()
    {
        MockHistoryPage(SummaryJson(), totalCount: 2);

        var cut = RenderComponent<Workouts>();

        // Polling wait (WaitForState) instead of render-event WaitForAssertion:
        // under load the data-carrying render can land between WaitForAssertion
        // creation and its next render event, timing out despite the data being
        // present. WaitForState re-checks every 100 ms regardless of renders.
        cut.WaitForState(() => cut.FindAll(".hs-workout-session-card").Count == 2);

        cut.Markup.Should().Contain("Push");
        cut.Markup.Should().Contain("Pull");
        // DESIGN.md §5.5: name line carries the leading muscles, the hero
        // metric sits on the right as value + label.
        cut.Markup.Should().Contain("Chest");
        cut.Find(".hs-workout-metric-value").TextContent.Trim().Should().Be("2,560 kg");
        cut.Find(".hs-workout-metric-label").TextContent.Trim().Should().Be("volume");
        cut.Markup.Should().NotContain("fixed-create-button");
    }

    [Fact]
    public void Renders_Activity_Calendar_Strips_And_Weekly_Tiles()
    {
        MockHistoryPage(SummaryJson(), totalCount: 2);
        MockHttp.When(HttpMethod.Get, "/Workouts/weekly-summary?weeks=1")
            .Respond("application/json", $$"""
                {
                  "weeks": [],
                  "activityDays": [
                    { "date": "{{DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd")}}T00:00:00", "workoutCount": 1, "exerciseCount": 6 },
                    { "date": "{{DateTime.Today.ToString("yyyy-MM-dd")}}T00:00:00", "workoutCount": 2, "exerciseCount": 11 }
                  ]
                }
                """);

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
        {
            var days = cut.FindAll(".hs-history-cal-day");
            days.Should().HaveCount(7, "the strip is a fixed 7-day window");
            cut.FindAll(".hs-history-cal-day.is-active").Should().HaveCount(2,
                "trained days get the accent dot");
            cut.FindAll(".hs-history-cal-day.is-today").Should().HaveCount(1,
                "the current day is highlighted");
            cut.Find(".hs-history-tile-value").TextContent.Trim().Should().Be("3",
                "entrenos 7d sums the workoutCount of the window");
            cut.FindAll(".hs-history-tile").Should().HaveCount(2,
                "entrenos 7d + current streak");
        });
    }

    [Fact]
    public void Activity_Calendar_Degrades_When_Weekly_Summary_Fails()
    {
        MockHistoryPage(SummaryJson(), totalCount: 2);
        MockHttp.When(HttpMethod.Get, "/Workouts/weekly-summary?weeks=1")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<Workouts>();

        // The strip is supplementary: a failed summary must not take the
        // history list down with it.
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-workout-session-card").Should().HaveCount(2);
            cut.FindAll(".hs-history-cal-day").Should().HaveCount(7);
            cut.FindAll(".hs-history-cal-day.is-active").Should().BeEmpty();
        });
    }

    [Fact]
    public void Renders_EmptyState_When_No_Workouts()
    {
        MockHistoryPage("[]", totalCount: 0);

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("No workouts yet"));
    }

    [Fact]
    public void Expands_Workout_And_Loads_Exercise_Details()
    {
        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
            cut.Find("button[aria-label='Show Push workout details']")
                .Should().NotBeNull());

        cut.Find("button[aria-label='Show Push workout details']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().Contain("4 sets × 8 reps");
            cut.Markup.Should().Contain("80.0 kg");
            cut.Find("a.hs-workout-add-exercise")
                .GetAttribute("href")
                .Should().Be(
                    $"/exercises/create?workoutId={PushWorkoutId}&returnUrl=%2Ftraining%2Fhistory&date=2025-06-01");
            cut.Find("a.hs-workout-edit-link")
                .GetAttribute("href")
                .Should().StartWith(
                    "/training/history/10000000-0000-0000-0000-000000000001/edit?returnUrl=");
            cut.Find("a.hs-workout-progression-link")
                .GetAttribute("href")
                .Should().StartWith("/training/library/1?returnUrl=");
        });
    }

    [Fact]
    public void Renders_Error_State_When_History_Request_Fails()
    {
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=20")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Couldn't load your workouts");
            cut.Markup.Should().Contain("Retry");
        });
    }

    [Fact]
    public void Loads_The_Next_Workout_Page()
    {
        MockHistoryPage(SummaryJson(includePull: false), totalCount: 2);
        MockHttp.When(HttpMethod.Get, "/workouts?page=2&pageSize=20")
            .Respond(_ => JsonResponse($"[{PullSummaryJson()}]", totalCount: 2));

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Load more")));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Load more"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-workout-session-card").Should().HaveCount(2);
            cut.Markup.Should().Contain("Pull");
            cut.Markup.Should().Contain("2 workouts");
        });
    }

    [Fact]
    public void Edits_A_Workout_From_Its_Expanded_Card()
    {
        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());
        MockHttp.When(HttpMethod.Put, "/workouts")
            .Respond(
                "application/json",
                DetailJson(type: "Upper body", date: "2025-06-03T00:00:00"));

        var cut = RenderComponent<Workouts>();
        ExpandPushWorkout(cut);

        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-workout-action")
                .Should().Contain(button => button.TextContent.Trim() == "Edit workout"));
        cut.FindAll(".hs-workout-action")
            .Single(button => button.TextContent.Trim() == "Edit workout")
            .Click();

        cut.WaitForAssertion(() =>
            cut.Find("[role='dialog']").TextContent.Should().Contain("Edit workout"));
        cut.Find("#edit-workout-date").Change("2025-06-03");
        cut.Find("#edit-workout-type").Change("Upper body");
        cut.Find(".hs-workout-sheet form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[role='dialog']").Should().BeEmpty();
            cut.Markup.Should().Contain("Upper body");
            cut.Markup.Should().Contain("Workout updated.");
        });
    }

    [Fact]
    public void Deletes_A_Workout_After_Explicit_Confirmation()
    {
        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());
        MockHttp.When(HttpMethod.Delete, $"/workouts/{PushWorkoutId}")
            .Respond(HttpStatusCode.NoContent);

        var cut = RenderComponent<Workouts>();
        ExpandPushWorkout(cut);

        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-workout-action")
                .Should().Contain(button => button.TextContent.Trim() == "Delete"));
        cut.FindAll(".hs-workout-action")
            .Single(button => button.TextContent.Trim() == "Delete")
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='dialog']").TextContent
                .Should().Contain("permanently deletes the workout and its");
            cut.Find("[role='dialog']").TextContent
                .Should().Contain("1 logged exercise record");
        });
        cut.Find(".hs-workout-sheet-button.is-danger").Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-workout-session-card").Should().BeEmpty();
            cut.Markup.Should().Contain("No workouts yet");
            cut.Markup.Should().Contain("Workout deleted.");
        });
    }

    [Fact]
    public void Keeps_Delete_Sheet_Open_When_Api_Returns_Problem_Details()
    {
        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());
        MockHttp.When(HttpMethod.Delete, $"/workouts/{PushWorkoutId}")
            .Respond(
                HttpStatusCode.Conflict,
                "application/problem+json",
                """{"title":"The workout could not be deleted."}""");

        var cut = RenderComponent<Workouts>();
        ExpandPushWorkout(cut);

        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-workout-action")
                .Should().Contain(button => button.TextContent.Trim() == "Delete"));
        cut.FindAll(".hs-workout-action")
            .Single(button => button.TextContent.Trim() == "Delete")
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='dialog']").TextContent
                .Should().Contain("Delete workout?");
            cut.Find(".hs-workout-sheet-button.is-danger")
                .Should().NotBeNull();
        });
        cut.Find(".hs-workout-sheet-button.is-danger").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='dialog']").Should().NotBeNull();
            cut.Find("[role='alert']").TextContent
                .Should().Contain("The workout could not be deleted.");
            cut.FindAll(".hs-workout-session-card").Should().ContainSingle();
        });
    }

    private void MockHistoryPage(string json, int totalCount)
        => MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=20")
            .Respond(_ => JsonResponse(json, totalCount));

    private static HttpResponseMessage JsonResponse(string json, int totalCount)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Total-Count", totalCount.ToString());
        return response;
    }

    private static string SummaryJson(bool includePull = true)
        => includePull
            ? $"[{PushSummaryJson()},{PullSummaryJson()}]"
            : $"[{PushSummaryJson()}]";

    private static string PushSummaryJson()
        => $$"""
            {
              "id": "{{PushWorkoutId}}",
              "date": "2025-06-01T00:00:00",
              "type": "Push",
              "exerciseCount": 1,
              "totalVolume": 2560,
              "leadingMuscles": ["Chest"],
              "exercises": []
            }
            """;

    private static string PullSummaryJson()
        => $$"""
            {
              "id": "{{PullWorkoutId}}",
              "date": "2025-06-02T00:00:00",
              "type": "Pull",
              "exerciseCount": 1,
              "totalVolume": 3000,
              "leadingMuscles": ["Back"],
              "exercises": []
            }
            """;

    private static string DetailJson(
        string type = "Push",
        string date = "2025-06-01T00:00:00")
        => $$"""
            {
              "id": "{{PushWorkoutId}}",
              "date": "{{date}}",
              "type": "{{type}}",
              "exerciseCount": 1,
              "totalVolume": 2560,
              "leadingMuscles": ["Chest"],
              "exercises": [
                {
                  "id": "10000000-0000-0000-0000-000000000001",
                  "typeId": 1,
                  "name": "Bench Press",
                  "series": 4,
                  "repetitions": 8,
                  "weight": 80,
                  "volume": 2560,
                  "muscles": ["Chest"]
                }
              ]
            }
            """;

    private static void ExpandPushWorkout(IRenderedComponent<Workouts> cut)
    {
        cut.WaitForAssertion(
            () => cut.Find("button[aria-label='Show Push workout details']")
                .Should().NotBeNull(),
            TimeSpan.FromSeconds(10));
        cut.Find("button[aria-label='Show Push workout details']").Click();
    }

    private static readonly Guid DuplicatedWorkoutId =
        Guid.Parse("00000000-0000-0000-0000-000000000099");

    [Fact]
    public void Duplicating_To_Today_Lands_On_Today_Instead_Of_Back_In_History()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost:7278/training/history");

        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());
        var duplicateCall = MockHttp.When(
                HttpMethod.Post,
                $"/workouts/{PushWorkoutId}/duplicate")
            .Respond(
                HttpStatusCode.Created,
                "application/json",
                DuplicatedJson(DateTime.Today.ToString("yyyy-MM-dd")));

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() => ExpandPushWorkout(cut), TimeSpan.FromSeconds(20));
        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-workout-action")
                .Single(button => button.TextContent.Trim() == "Duplicate")
                .Click(), TimeSpan.FromSeconds(20));

        cut.WaitForAssertion(
            () => cut.Find(".hs-workout-sheet-actions .is-primary").Click(),
            TimeSpan.FromSeconds(20));

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(duplicateCall).Should().Be(1);

            // Rule 2 (ux-ui-good-apps): land in the user's context, not the catalog.
            navigationManager.Uri.Should().EndWith("/training/today");
            Services.GetRequiredService<ISnackbar>()
                .ShownSnackbars
                .Should().Contain(snackbar => snackbar.Message == "Workout copied to today.");
        }, TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void Duplicating_To_Another_Date_Stays_In_History_With_The_Copy_Expanded()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost:7278/training/history");

        MockHistoryPage(SummaryJson(includePull: false), totalCount: 1);
        MockHttp.When(HttpMethod.Get, $"/workouts/{PushWorkoutId}")
            .Respond("application/json", DetailJson());
        var duplicateCall = MockHttp.When(
                HttpMethod.Post,
                $"/workouts/{PushWorkoutId}/duplicate")
            .Respond(
                HttpStatusCode.Created,
                "application/json",
                DuplicatedJson("2025-06-10"));

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() => ExpandPushWorkout(cut), TimeSpan.FromSeconds(20));
        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-workout-action")
                .Single(button => button.TextContent.Trim() == "Duplicate")
                .Click(), TimeSpan.FromSeconds(20));

        cut.WaitForAssertion(() =>
        {
            cut.Find("#duplicate-workout-date").Change("2025-06-10");
            cut.Find(".hs-workout-sheet-actions .is-primary").Click();
        }, TimeSpan.FromSeconds(20));

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(duplicateCall).Should().Be(1);

            // The sheet is closed and the user stays on their copy, not on a list.
            navigationManager.Uri.Should().EndWith("/training/history");
            cut.FindAll(".hs-workout-sheet").Should().BeEmpty();
            cut.Markup.Should().Contain("Workout duplicated.");

            var copyCard = cut.Find(
                $"[id='workout-card-{DuplicatedWorkoutId:N}']");
            copyCard.ClassList.Should().Contain("is-expanded");
        }, TimeSpan.FromSeconds(20));
    }

    private static string DuplicatedJson(string date)
        => $$"""
            {
              "sourceWorkoutId": "{{PushWorkoutId}}",
              "workout": {
                "id": "{{DuplicatedWorkoutId}}",
                "date": "{{date}}T00:00:00",
                "type": "Push",
                "exerciseCount": 1,
                "totalVolume": 2560,
                "leadingMuscles": ["Chest"],
                "exercises": [
                  {
                    "id": "20000000-0000-0000-0000-000000000001",
                    "typeId": 1,
                    "name": "Bench Press",
                    "series": 4,
                    "repetitions": 8,
                    "weight": 80,
                    "volume": 2560,
                    "muscles": ["Chest"]
                  }
                ]
              }
            }
            """;
}
