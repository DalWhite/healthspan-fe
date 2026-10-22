using System.Net;
using System.Text;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Universal tracker UI on Today + History. Rules applied (ux-ui-good-apps):
/// intent is one tap, blocks stay behind one tap (progressive disclosure),
/// attempt progress is glanceable, and legacy sessions render exactly as before.
/// </summary>
public class TodayTrackerUiTests : BlazorTestBase
{

    /// <summary>
    /// Today re-renders while its cached data refresh lands, which replaces nodes
    /// found earlier; a click on a detached node silently does nothing. Re-render
    /// before each attempt, re-find the button, and stop as soon as the action is
    /// visible so a click is never applied twice.
    /// </summary>
    /// <summary>
    /// Today re-renders while its cached data refresh lands, which replaces nodes
    /// found earlier; a click on a detached node silently does nothing. Re-render
    /// before each attempt, re-find the button, and stop as soon as the action is
    /// visible so a click is never applied twice.
    /// </summary>
    private void ClickButtonUntil<TComponent>(
        Bunit.IRenderedComponentBase<TComponent> cut,
        string buttonText,
        Func<bool> actionIsVisible)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        var queries = (Bunit.IRenderedFragment)cut;

        for (var attempt = 0; attempt < 6 && !actionIsVisible(); attempt++)
        {
            cut.SetParametersAndRender();
            queries.FindAll("button")
                .Single(button => button.TextContent.Trim() == buttonText)
                .Click();
        }

        actionIsVisible().Should().BeTrue();
    }

    [Fact]
    public void Training_Type_And_Goal_Are_One_Tap_And_Save_Themselves()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts*")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "date": "{{DateTime.Today:yyyy-MM-dd}}",
                        "type": "Training",
                        "exercises": [],
                        "blocks": [],
                        "recentExerciseTypeIds": []
                    }
                ]
                """);
        var patchCall = MockHttp.When(HttpMethod.Patch, $"/Workouts/{workoutId}")
            .WithContent("""{"goal":"Hypertrophy","modality":null}""")
            .Respond("application/json", "{}");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Build muscle"), TimeSpan.FromSeconds(10));

        // Progressive disclosure: the common training types are upfront, the long
        // tail is behind one "Otros" tap instead of a 10-chip wall.
        cut.FindAll("button").Should().Contain(button => button.TextContent.Trim() == "Otros");

        ClickButtonUntil(
            cut,
            "Build muscle",
            () => cut.FindAll(".hs-chip-selected")
                .Any(button => button.TextContent.Trim() == "Build muscle"));

        // The exact wire payload is matched by the mock itself: the FE must send
        // the API's enum names, never a renamed value.
        cut.WaitForAssertion(() => MockHttp.GetMatchCount(patchCall).Should().Be(1), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Block_Creator_Stays_Behind_One_Tap_And_Creates_A_Block()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts*")
            .Respond("application/json", "[]");
        // Today re-reads the session after every write, so the fake server must
        // carry the block once it exists — otherwise the refresh wipes it and the
        // test asserts against a markup that was never meant to lose it.
        var blockId = Guid.NewGuid();
        var blockOnServer = false;

        string TodayPayload(bool withBlock) => $$"""
            [
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [],
                    "blocks": [
                        { "id": "{{blockId}}", "order": 1, "type": "Superset", "label": null, "rounds": 4, "exercises": [] }
                    ],
                    "recentExerciseTypeIds": []
                }
            ]
            """;

        string TodayPayloadEmpty() => $$"""
            [
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [],
                    "blocks": [],
                    "recentExerciseTypeIds": []
                }
            ]
            """;

        MockHttp.When(HttpMethod.Get, $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    blockOnServer ? TodayPayload(true) : TodayPayloadEmpty(), Encoding.UTF8, "application/json")
            });
        var blockCall = MockHttp.When(HttpMethod.Post, $"/Workouts/{workoutId}/blocks")
            .Respond(request =>
            {
                var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                body.Should().Contain("\"type\":\"Superset\"");
                blockOnServer = true;
                // The API answers a block create with the whole session (MapSession),
                // so the mock must too: the FE adopts the session, not a lone block.
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent($$"""
                        {
                            "id": "{{workoutId}}",
                            "date": "{{DateTime.Today:yyyy-MM-dd}}",
                            "type": "Training",
                            "exercises": [],
                            "blocks": [
                                { "id": "{{blockId}}", "order": 1, "type": "Superset", "label": null, "rounds": 4, "exercises": [] }
                            ],
                            "recentExerciseTypeIds": []
                        }
                        """, Encoding.UTF8, "application/json")
                };
            });

        var cut = RenderComponent<TodayWorkout>();

        // Wait for the loaded session, not just the form: Today still has a request
        // in flight before that, and its render would replace the clicked node.
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Nueva sesión"), TimeSpan.FromSeconds(10));

        // Rule 3: the block machinery is not on screen until it is asked for.
        cut.FindAll(".hs-today-block-creator").Should().BeEmpty();

        ClickButtonUntil(
            cut,
            "Bloque",
            () => cut.FindAll("button").Any(button =>
                button.TextContent.Trim() == "Superserie"));

        cut.FindAll("button").Should().Contain(button =>
            button.TextContent.Trim() == "Circuito");

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Superserie")
            .Click();
        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Añadir bloque")
            .Click();

        cut.WaitForAssertion(() => MockHttp.GetMatchCount(blockCall).Should().Be(1), TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Superserie"), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Attempt_Progress_Is_Glanceable_And_The_Tick_Saves_It()
    {
        var workoutId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        string? putBody = null;

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts*")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "date": "{{DateTime.Today:yyyy-MM-dd}}",
                        "type": "Training",
                        "goal": "Power",
                        "modality": "Pliometria",
                        "exercises": [
                            {
                                "id": "{{exerciseId}}",
                                "typeId": 1,
                                "name": "Box jump",
                                "series": 0,
                                "repetitions": 0,
                                "weight": 0,
                                "attempts": 8,
                                "completedAttempts": 3,
                                "qualityIntent": "Explosive",
                                "loadType": "Bodyweight"
                            }
                        ],
                        "blocks": [],
                        "recentExerciseTypeIds": [1]
                    }
                ]
                """);
        var putCall = MockHttp.When(HttpMethod.Put, "/Exercises")
            .Respond(request =>
            {
                putBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            // The count is the status: no progress bar, no dialog.
            cut.Markup.Should().Contain("3/8");
            cut.Markup.Should().Contain("Explosive");
            cut.Markup.Should().Contain("Bodyweight");
        }, TimeSpan.FromSeconds(10));

        cut.Find(".hs-today-attempt-plus").Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("4/8"), TimeSpan.FromSeconds(10));

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(putCall).Should().Be(1);
            putBody.Should().Contain("\"completedAttempts\":4");
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Legacy_Exercise_Renders_Without_Tracker_Controls()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts*")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "date": "{{DateTime.Today:yyyy-MM-dd}}",
                        "type": "Training",
                        "exercises": [
                            {
                                "id": "{{Guid.NewGuid()}}",
                                "typeId": 1,
                                "name": "Bench Press",
                                "series": 3,
                                "repetitions": 10,
                                "weight": 60,
                                "attempts": 1,
                                "completedAttempts": 1
                            }
                        ],
                        "blocks": [],
                        "recentExerciseTypeIds": [1]
                    }
                ]
                """);

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            // The set table is the log: one row per series, reps and load inline.
            var rows = cut.FindAll(".hs-settable-row");
            rows.Should().HaveCount(3);
            cut.FindAll(".hs-settable-input")
                .Select(input => input.GetAttribute("value"))
                .Should().Contain("10");
        }, TimeSpan.FromSeconds(10));

        // Backward compatibility: an old session must not grow new controls.
        cut.FindAll("button").Should().NotContain(button =>
            button.TextContent.Trim() == "+1 done");
        cut.FindAll(".hs-today-attempt-row").Should().BeEmpty();
    }

    [Fact]
    public void History_Groups_Exercises_Under_Block_Headers_And_Shows_The_Intent()
    {
        var workoutId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=20")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "date": "2026-10-20T00:00:00",
                        "type": "Training",
                        "goal": "Hypertrophy",
                        "modality": "Fuerza",
                        "perceivedFatigue": 4,
                        "exerciseCount": 2,
                        "totalVolume": 0,
                        "leadingMuscles": []
                    }
                ]
                """);
        MockHttp.When(HttpMethod.Get, $"/workouts/{workoutId}")
            .Respond("application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "2026-10-20T00:00:00",
                    "type": "Training",
                    "goal": "Hypertrophy",
                    "modality": "Fuerza",
                    "perceivedFatigue": 4,
                    "exerciseCount": 2,
                    "totalVolume": 0,
                    "leadingMuscles": [],
                    "blocks": [
                        { "id": "{{blockId}}", "order": 1, "type": "Superset", "label": null, "rounds": 4 }
                    ],
                    "exercises": [
                        {
                            "id": "{{Guid.NewGuid()}}",
                            "typeId": 1,
                            "name": "Bench Press",
                            "series": 4,
                            "repetitions": 8,
                            "weight": 60,
                            "blockId": "{{blockId}}",
                            "attempts": 4,
                            "completedAttempts": 4
                        },
                        {
                            "id": "{{Guid.NewGuid()}}",
                            "typeId": 2,
                            "name": "Row",
                            "series": 4,
                            "repetitions": 8,
                            "weight": 40,
                            "blockId": "{{blockId}}",
                            "attempts": 4,
                            "completedAttempts": 4
                        }
                    ]
                }
                """);

        var cut = RenderComponent<Workouts>();

        cut.WaitForAssertion(() =>
        {
            // Trust infrastructure: the intent is visible without opening the workout.
            cut.Markup.Should().Contain("Strength");
            cut.Markup.Should().Contain("Build muscle");
            cut.Markup.Should().Contain("Fatigue 4/5");
        }, TimeSpan.FromSeconds(10));

        for (var attempt = 0;
             attempt < 6 && !cut.Markup.Contains("hs-workout-block-type");
             attempt++)
        {
            cut.SetParametersAndRender();
            cut.Find(".hs-workout-session-toggle").Click();
        }

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-workout-block-type")
                .Select(node => node.TextContent.Trim())
                .Should().Contain("Superset");
            cut.FindAll(".hs-workout-block-rounds")
                .Select(node => node.TextContent.Trim())
                .Should().Contain("4 rounds");
        }, TimeSpan.FromSeconds(10));
    }
}
