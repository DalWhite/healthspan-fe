using System.Net;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class TodayWorkoutTests : BlazorTestBase
{
    [Fact]
    public void Starts_A_New_Workout_Without_Leaving_Today()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond(HttpStatusCode.NoContent);
        MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(HttpStatusCode.Created, "application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [],
                    "recentExerciseTypeIds": []
                }
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Start workout"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start workout"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No exercises yet.");
            cut.Markup.Should().Contain("Add exercise");
            cut.Markup.Should().Contain("Finish");
        });
    }

    [Fact]
    public void Resumes_Existing_Workout_With_Compact_Summary()
    {
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", """
                [
                    {
                        "id": 1,
                        "name": "Bench Press",
                        "isFavourite": true,
                        "weightType": 0
                    }
                ]
                """);
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [
                        {
                            "id": "{{exerciseId}}",
                            "typeId": 1,
                            "name": "Bench Press",
                            "series": 3,
                            "repetitions": 8,
                            "weight": 80
                        }
                    ],
                    "recentExerciseTypeIds": [1]
                }
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().Contain("1 exercise · 3 sets");
            cut.Markup.Should().Contain("3 × 8 · 80 kg");
            cut.Markup.Should().Contain("Favourites");
            cut.Find("a[aria-label='Edit Bench Press entry']")
                .GetAttribute("href")
                .Should().Be(
                    $"/training/history/{exerciseId}/edit?returnUrl=%2Ftraining%2Ftoday");
        });
    }

    [Fact]
    public void Shows_BodyWeight_And_Added_Load_Separately()
    {
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [
                        {
                            "id": "{{exerciseId}}",
                            "typeId": 1,
                            "name": "Weighted Pull-up",
                            "series": 3,
                            "repetitions": 5,
                            "weight": 85,
                            "bodyWeight": 80,
                            "addedWeight": 5
                        }
                    ],
                    "recentExerciseTypeIds": []
                }
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Weighted Pull-up");
            cut.Markup.Should().Contain(
                "3 × 5 · 80 kg BW + 5 kg");
        });
    }

    [Fact]
    public void StartWorkout_Malformed_Response_Shows_One_Error()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond(HttpStatusCode.NoContent);
        MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(HttpStatusCode.Created, "application/json", "{not-json}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Start workout"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start workout"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("workout response was incomplete");
        });
    }

    [Fact]
    public void StartWorkout_Timeout_Shows_One_Error()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond(HttpStatusCode.NoContent);
        MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(_ => throw new TaskCanceledException());

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Start workout"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start workout"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("Starting took too long");
        });
    }

    [Fact]
    public void LoadWorkout_Malformed_Response_Shows_One_Error()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "{not-json}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("workout response was incomplete");
            cut.Markup.Should().Contain("Retry");
        });
    }
}
