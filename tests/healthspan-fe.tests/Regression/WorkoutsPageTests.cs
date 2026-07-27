using System.Net;
using System.Text;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages;
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

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-workout-session-card").Should().HaveCount(2);
            cut.Markup.Should().Contain("Push");
            cut.Markup.Should().Contain("Pull");
            cut.Markup.Should().Contain("1 exercise");
            cut.Markup.Should().Contain("2,560 kg volume");
            cut.Markup.Should().Contain("Chest");
            cut.Markup.Should().NotContain("fixed-create-button");
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
            cut.Find("a[href='/training/library/1']").Should().NotBeNull();
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
                .Should().Contain(button => button.TextContent.Trim() == "Edit"));
        cut.FindAll(".hs-workout-action")
            .Single(button => button.TextContent.Trim() == "Edit")
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
        cut.WaitForAssertion(() =>
            cut.Find("button[aria-label='Show Push workout details']")
                .Should().NotBeNull());
        cut.Find("button[aria-label='Show Push workout details']").Click();
    }
}
