using System.Net;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class TodayWorkoutTests : BlazorTestBase
{
    [Fact]
    public void Today_Logs_The_First_Exercise_Without_A_Start_Session_Tap()
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
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=10")
            .Respond("application/json", "[]");
        var startCall = MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(HttpStatusCode.Created, "application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [],
                    "recentExerciseTypeIds": []
                }
                """);
        var addCall = MockHttp.When(HttpMethod.Post, $"/Workouts/{workoutId}/exercises")
            .Respond(HttpStatusCode.OK, "application/json", $$"""
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
                            "repetitions": 10,
                            "weight": 0
                        }
                    ],
                    "recentExerciseTypeIds": [1]
                }
                """);

        var cut = RenderComponent<TodayWorkout>();

        // Rule 1 (ux-ui-good-apps): the activation action is on the page before
        // any decision is asked. No "Start workout" gate, no session needed.
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Add exercise");
            cut.FindAll("button")
                .Should().NotContain(button =>
                    button.TextContent.Contains("Start workout"));
        });

        // Tap 1: pick the exercise. Tap 2: Add exercise. Sets/reps arrive prefilled.
        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Bench Press")));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Bench Press"))
            .Click();
        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Add exercise")
            .Click();

        cut.WaitForAssertion(() =>
        {
            // The session appears only because the user actually logged something.
            MockHttp.GetMatchCount(startCall).Should().Be(1);
            MockHttp.GetMatchCount(addCall).Should().Be(1);

            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().Contain("3 × 10");

            Services.GetRequiredService<ISnackbar>()
                .ShownSnackbars
                .Should().Contain(snackbar => snackbar.Message == "Exercise added.");
        }, TimeSpan.FromSeconds(10));
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
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [{
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
                }]
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
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [{
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
                }]
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
        var existingWorkoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", TodaySessionJson(existingWorkoutId, Guid.NewGuid()));
        MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(HttpStatusCode.Created, "application/json", "{not-json}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("New session"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("New session"))
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
        var existingWorkoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", TodaySessionJson(existingWorkoutId, Guid.NewGuid()));
        MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond(_ => throw new TaskCanceledException());

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("New session"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("New session"))
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
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "{not-json}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("workout response was incomplete");
            cut.Markup.Should().Contain("Retry");
        });
    }

    [Fact]
    public void Shows_Repeat_Last_Workout_For_The_Most_Recent_Workout_With_Exercises()
    {
        var emptyWorkoutId = Guid.NewGuid();
        var lastWorkoutId = Guid.NewGuid();
        var lastWorkoutDate = DateTime.Today.AddDays(-7);

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]"); // no session for today
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=10")
            .Respond("application/json", $$"""
                [
                    {
                      "id": "{{emptyWorkoutId}}",
                      "date": "{{DateTime.Today:yyyy-MM-dd}}",
                      "type": "Rest placeholder",
                      "exerciseCount": 0,
                      "totalVolume": 0,
                      "leadingMuscles": [],
                      "exercises": []
                    },
                    {
                      "id": "{{lastWorkoutId}}",
                      "date": "{{lastWorkoutDate:yyyy-MM-dd}}",
                      "type": "Cycling",
                      "exerciseCount": 2,
                      "totalVolume": 0,
                      "leadingMuscles": [],
                      "exercises": []
                    }
                ]
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button")
                .Should().ContainSingle(btn => btn.TextContent.Trim() == "Repeat last workout");
            cut.Markup.Should().Contain($"From {lastWorkoutDate:ddd, d MMM}");
            cut.Markup.Should().Contain("2 exercises");
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Repeat_Last_Workout_Copies_It_To_Today_And_Keeps_The_User_On_Today()
    {
        var lastWorkoutId = Guid.NewGuid();
        var duplicatedWorkoutId = Guid.NewGuid();
        var duplicatedExerciseId = Guid.NewGuid();
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost:7278/training/today");

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        var sessionLoads = 0;
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond(_ =>
            {
                // First load: nothing logged today yet. After the duplicate the
                // copy shows up as today's session.
                sessionLoads++;
                var body = sessionLoads == 1 ? "[]" : TodaySessionJson(duplicatedWorkoutId, duplicatedExerciseId);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            });
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=10")
            .Respond("application/json", $$"""
                [{
                    "id": "{{lastWorkoutId}}",
                    "date": "{{DateTime.Today.AddDays(-7):yyyy-MM-dd}}",
                    "type": "Cycling",
                    "exerciseCount": 2,
                    "totalVolume": 0,
                    "leadingMuscles": [],
                    "exercises": []
                }]
                """);
        var duplicateCall = MockHttp.When(
                HttpMethod.Post,
                $"/Workouts/{lastWorkoutId}/duplicate")
            .Respond(HttpStatusCode.Created, "application/json", $$"""
                {
                    "sourceWorkoutId": "{{lastWorkoutId}}",
                    "workout": {
                        "id": "{{duplicatedWorkoutId}}",
                        "date": "{{DateTime.Today:yyyy-MM-dd}}",
                        "type": "Cycling",
                        "exerciseCount": 2,
                        "totalVolume": 0,
                        "leadingMuscles": [],
                        "exercises": []
                    }
                }
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Single(btn => btn.TextContent.Trim() == "Repeat last workout")
                .Click(), TimeSpan.FromSeconds(10));

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(duplicateCall).Should().Be(1);

            // Rule 2 (ux-ui-good-apps): the user lands on their own context,
            // with the copied exercises ready to edit, not on a catalog.
            navigationManager.Uri.Should().EndWith("/training/today");
            cut.Markup.Should().Contain("Bike");
            cut.Markup.Should().Contain("Add exercise");
            cut.Markup.Should().NotContain("Repeat last workout");

            Services.GetRequiredService<ISnackbar>()
                .ShownSnackbars
                .Should().Contain(snackbar => snackbar.Message == "Last workout copied to today.");
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Repeat_Last_Workout_Keeps_The_User_On_Today_When_The_Copy_Fails()
    {
        var lastWorkoutId = Guid.NewGuid();
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost:7278/training/today");

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=10")
            .Respond("application/json", $$"""
                [{
                    "id": "{{lastWorkoutId}}",
                    "date": "{{DateTime.Today.AddDays(-2):yyyy-MM-dd}}",
                    "type": "Cycling",
                    "exerciseCount": 2,
                    "totalVolume": 0,
                    "leadingMuscles": [],
                    "exercises": []
                }]
                """);
        MockHttp.When(
                HttpMethod.Post,
                $"/Workouts/{lastWorkoutId}/duplicate")
            .Respond(HttpStatusCode.BadRequest, "application/json", "{}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Single(btn => btn.TextContent.Trim() == "Repeat last workout")
                .Click(), TimeSpan.FromSeconds(10));

        cut.WaitForAssertion(() =>
        {
            navigationManager.Uri.Should().EndWith("/training/today");
            cut.Markup.Should().Contain("could not be repeated");
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Entry_Without_Sets_And_Reps_Invites_The_Detail_Instead_Of_Zero()
    {
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [{
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [
                        {
                            "id": "{{exerciseId}}",
                            "typeId": 1,
                            "name": "Bench Press",
                            "series": null,
                            "repetitions": null,
                            "weight": null
                        }
                    ],
                    "recentExerciseTypeIds": [1]
                }]
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            // A logged entry without numbers never reads as a fake "0 x 0".
            cut.Markup.Should().Contain("Add sets and reps");
            cut.Markup.Should().NotContain("0 × 0");
            cut.Find("a[aria-label='Edit Bench Press entry']");
        });
    }

    private static string TodaySessionJson(Guid workoutId, Guid exerciseId)
        => $$"""
            [{
                "id": "{{workoutId}}",
                "date": "{{DateTime.Today:yyyy-MM-dd}}",
                "type": "Cycling",
                "exercises": [
                    {
                        "id": "{{exerciseId}}",
                        "typeId": 1,
                        "name": "Bike",
                        "series": 3,
                        "repetitions": 10,
                        "weight": 0
                    }
                ],
                "recentExerciseTypeIds": [1]
            }]
            """;
}
