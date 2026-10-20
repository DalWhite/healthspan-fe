using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Pages;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using healthspan_fe_wasm_pwa.Pages.ExerciseTypes;
using healthspan_fe_wasm_pwa.Shared;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Renders the training pages with deterministic fixtures and, when
/// HS_TOUCH_HARNESS_DIR is set, writes their markup to disk so the touch-target
/// audit can measure real rendered controls against the real stylesheet.
/// Outside the audit run it stays a plain regression test: the controls that the
/// touch-target baseline must cover are asserted to exist.
/// </summary>
public class TouchTargetHarnessTests : BlazorTestBase
{
    private static readonly Guid WorkoutId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ExerciseId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    [Fact]
    public void Training_Pages_Render_The_Controls_The_Touch_Target_Baseline_Covers()
    {
        var markup = RenderTrainingMarkup();

        markup["appbar"].Should().Contain("hs-appbar-primary-action");
        markup["today"].Should().Contain("hs-today-exercise-edit-link");
        markup["today"].Should().Contain("hs-today-session-switcher");
        markup["today"].Should().Contain("hs-today-routine-link");
        markup["entryform"].Should().Contain("hs-exercise-quick-button");
        markup["history"].Should().Contain("hs-workout-session-toggle");
        markup["history"].Should().Contain("hs-workout-action");
        markup["library"].Should().Contain("hs-et-action-slot");
        markup["library"].Should().Contain("hs-et-variation-toggle");

        var dir = Environment.GetEnvironmentVariable("HS_TOUCH_HARNESS_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        foreach (var (name, html) in markup)
        {
            File.WriteAllText(Path.Combine(dir, $"{name}.html"), html);
        }
    }

    [Fact]
    public void Stylesheet_Keeps_The_44px_Touch_Target_Baseline_On_The_Training_Shells()
    {
        var css = ReadAppCss();

        css.Should().Contain("--hs-touch-target: 44px",
            "the touch-target baseline must stay one token so it can be tuned in one place");

        foreach (var shell in new[]
                 {
                     ".hs-appbar", ".hs-page-shell", ".hs-health-shell",
                     ".hs-page-toolbar", ".hs-login-layout"
                 })
        {
            css.Should().Contain($"{shell} :is(.mud-button-root",
                $"{shell} must keep the shared 44px baseline for the controls it holds");
        }

        RulesShrinkingBelowBaseline(css, new[]
        {
            ".hs-et-variation-toggle", ".hs-et-card-menu", ".hs-exercise-quick-button",
            ".hs-history-tag-filter", ".hs-exercise-entry-submit", ".hs-exercise-edit-button",
            ".hs-today-routine-link",
        }).Should().BeEmpty(
            "training controls must not re-declare a touch target below --hs-touch-target");
    }

    private static string ReadAppCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "wwwroot", "css", "app.css");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("wwwroot/css/app.css was not found above the test assembly.");
    }

    private static IEnumerable<string> RulesShrinkingBelowBaseline(string css, IEnumerable<string> selectors)
    {
        var wanted = selectors.ToList();
        var offenders = new List<string>();

        foreach (Match block in Regex.Matches(css, @"([^{}]+)\{([^{}]*)\}"))
        {
            var selector = block.Groups[1].Value.Trim().Replace("\n", " ");
            if (!wanted.Any(w => selector.Contains(w)))
            {
                continue;
            }

            foreach (Match size in Regex.Matches(block.Groups[2].Value, @"min-(?:height|width):\s*(\d+(?:\.\d+)?)px"))
            {
                if (double.Parse(size.Groups[1].Value, CultureInfo.InvariantCulture) < 44)
                {
                    offenders.Add($"{selector} -> {size.Value.Trim()}");
                }
            }
        }

        return offenders;
    }

    private Dictionary<string, string> RenderTrainingMarkup()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost:7278/training/today");

        MockCatalog();
        MockToday();
        MockHistory();

        var rendered = new Dictionary<string, string>();

        var layout = RenderComponent<MainLayout>();
        rendered["appbar"] = layout.Markup;

        var today = RenderComponent<TodayWorkout>();
        today.WaitForAssertion(
            () => today.FindAll(".hs-today-exercise-edit-link").Count.Should().BeGreaterThan(0),
            TimeSpan.FromSeconds(10));
        rendered["today"] = today.Markup;

        var entryForm = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ShowQuickPicks, true)
            .Add(component => component.WorkoutId, WorkoutId)
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 1, Name = "Bench Press", IsFavourite = true },
                new GetExerciseTypeResource { Id = 2, Name = "Incline Bench Press" }
            })
            .Add(component => component.RecentExerciseTypeIds, new[] { 2 }));
        entryForm.WaitForAssertion(
            () => entryForm.FindAll(".hs-exercise-quick-button").Count.Should().BeGreaterThan(0),
            TimeSpan.FromSeconds(10));
        rendered["entryform"] = entryForm.Markup;

        var history = RenderComponent<Workouts>();
        history.WaitForAssertion(
            () => history.FindAll(".hs-workout-session-toggle").Count.Should().BeGreaterThan(0),
            TimeSpan.FromSeconds(10));
        history.Find("button[aria-label='Show Push workout details']").Click();
        history.WaitForAssertion(
            () => history.FindAll(".hs-workout-action").Count.Should().BeGreaterThan(0),
            TimeSpan.FromSeconds(10));
        rendered["history"] = history.Markup;

        var library = RenderComponent<ExerciseTypes>();
        library.WaitForAssertion(
            () => library.FindAll(".hs-et-action-slot").Count.Should().BeGreaterThan(0),
            TimeSpan.FromSeconds(10));
        rendered["library"] = library.Markup;

        return rendered;
    }

    private void MockCatalog()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", """
                [
                  {
                    "id": 1,
                    "name": "Bench Press",
                    "description": "Horizontal press pattern",
                    "muscleAssignments": [
                      { "muscleId": 11, "role": "Primary", "sortOrder": 1 }
                    ],
                    "isFavourite": true,
                    "parentExerciseTypeId": null,
                    "tags": [
                      { "name": "barbell", "category": "equipment" },
                      { "name": "push", "category": "type" }
                    ]
                  },
                  {
                    "id": 2,
                    "name": "Incline Bench Press",
                    "description": "Variation with different emphasis",
                    "muscleAssignments": [
                      { "muscleId": 11, "role": "Primary", "sortOrder": 1 }
                    ],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": [
                      { "name": "barbell", "category": "equipment" },
                      { "name": "variation", "category": "type" }
                    ]
                  },
                  {
                    "id": 3,
                    "name": "Decline Bench Press",
                    "muscleAssignments": [],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": []
                  },
                  {
                    "id": 4,
                    "name": "Close Grip Bench Press",
                    "muscleAssignments": [],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": []
                  },
                  {
                    "id": 5,
                    "name": "Wide Grip Bench Press",
                    "muscleAssignments": [],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": []
                  },
                  {
                    "id": 6,
                    "name": "Paused Bench Press",
                    "muscleAssignments": [],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": []
                  },
                  {
                    "id": 7,
                    "name": "Larsen Bench Press",
                    "muscleAssignments": [],
                    "isFavourite": false,
                    "parentExerciseTypeId": 1,
                    "tags": []
                  }
                ]
                """);
        MockHttp.When("/api/muscles")
            .Respond("application/json", """
                [
                  { "id": 11, "slug": "chest", "name": "Chest", "side": "front", "muscleGroup": "Upper body", "sortOrder": 110 }
                ]
                """);
    }

    private void MockToday()
    {
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", $$"""
                [{
                  "id": "{{WorkoutId}}",
                  "date": "{{DateTime.Today:yyyy-MM-dd}}",
                  "type": "Training",
                  "exercises": [
                    {
                      "id": "{{ExerciseId}}",
                      "typeId": 1,
                      "name": "Bench Press",
                      "series": 4,
                      "repetitions": 8,
                      "weight": 80
                    }
                  ],
                  "recentExerciseTypeIds": [1, 2]
                },
                {
                  "id": "00000000-0000-0000-0000-000000000002",
                  "date": "{{DateTime.Today:yyyy-MM-dd}}",
                  "type": "Training",
                  "exercises": [
                    {
                      "id": "10000000-0000-0000-0000-000000000002",
                      "typeId": 2,
                      "name": "Incline Bench Press",
                      "series": 3,
                      "repetitions": 10,
                      "weight": 60
                    }
                  ],
                  "recentExerciseTypeIds": [1, 2]
                }]
                """);

        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", $$"""
                {
                  "date": "{{DateTime.UtcNow:yyyy-MM-dd}}",
                  "pendingNow": true,
                  "routineId": "00000000-0000-0000-0000-0000000000a1",
                  "routineName": "Push / Pull",
                  "templateId": 7,
                  "type": "Push",
                  "frequencyPerWeek": 2,
                  "exercises": [
                    {
                      "exerciseTypeId": 1,
                      "exerciseTypeName": "Bench Press",
                      "description": "Barbell bench press",
                      "order": 1,
                      "sets": 4,
                      "reps": 8,
                      "loadKg": 80
                    }
                  ]
                }
                """);
    }

    private void MockHistory()
    {
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=20")
            .Respond(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [{
                          "id": "00000000-0000-0000-0000-000000000001",
                          "date": "2025-06-01T00:00:00",
                          "type": "Push",
                          "exerciseCount": 1,
                          "totalVolume": 2560,
                          "leadingMuscles": ["Chest"],
                          "exercises": []
                        }]
                        """, Encoding.UTF8)
                };
                response.Headers.Add("X-Total-Count", "1");
                return response;
            });
        MockHttp.When(HttpMethod.Get, $"/workouts/{WorkoutId}")
            .Respond("application/json", """
                {
                  "id": "00000000-0000-0000-0000-000000000001",
                  "date": "2025-06-01T00:00:00",
                  "type": "Push",
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
                """);
    }
}
