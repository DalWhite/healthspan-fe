using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Text;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
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
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=14")
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
            cut.Markup.Should().Contain("Añadir ejercicio");
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
            .Single(button => button.TextContent.Trim() == "Añadir ejercicio")
            .Click();

        cut.WaitForAssertion(() =>
        {
            // The session appears only because the user actually logged something.
            MockHttp.GetMatchCount(startCall).Should().Be(1);
            MockHttp.GetMatchCount(addCall).Should().Be(1);

            cut.Markup.Should().Contain("Bench Press");

            // The log is the interface: 3 prefilled set rows, each carrying 10 reps.
            cut.FindAll(".hs-settable tbody tr").Should().HaveCount(3);
            cut.Find("input[aria-label='Repeticiones de Bench Press']")
                .GetAttribute("value").Should().Be("10");

            Services.GetRequiredService<ISnackbar>()
                .ShownSnackbars
                .Should().Contain(snackbar => snackbar.Message == "Ejercicio añadido.");
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
            cut.Markup.Should().Contain("1 ejercicio · 3 series");
            cut.Markup.Should().Contain("Favoritos");

            // The set table is the log: 3 rows carrying the logged 8 reps @ 80 kg.
            cut.FindAll(".hs-settable tbody tr").Should().HaveCount(3);
            cut.Find("input[aria-label='Repeticiones de Bench Press']")
                .GetAttribute("value").Should().Be("8");
            cut.Find("input[aria-label='Peso en kg de Bench Press']")
                .GetAttribute("value").Should().Be("80");

            // The accent exercise name IS the edit affordance (DESIGN.md §2).
            cut.Find("a[aria-label='Editar Bench Press']")
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

            // Bodyweight work logs the extra load, never a misleading total.
            cut.Find("input[aria-label='Repeticiones de Weighted Pull-up']")
                .GetAttribute("value").Should().Be("5");
            cut.Find("input[aria-label='Peso añadido en kg de Weighted Pull-up']")
                .GetAttribute("value").Should().Be("5");
            cut.Find(".hs-settable-unit").TextContent.Should().Be("kg extra");
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

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Nueva sesión"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Nueva sesión"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("estaba incompleta");
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

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Nueva sesión"));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Nueva sesión"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("Tardó demasiado");
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
            cut.Markup.Should().Contain("estaba incompleta");
            cut.Markup.Should().Contain("Reintentar");
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
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=14")
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
            var repeat = cut.FindAll("button")
                .Single(btn => btn.GetAttribute("class")?.Contains("hs-cta-secondary") == true);
            // The CTA names what it repeats and when it happened (DESIGN.md §5.1).
            repeat.TextContent.Should().Contain("Repetir");
            cut.Markup.Should().Contain("7 días");
            cut.Markup.Should().Contain("2 ejercicios");
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
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=14")
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
        cut.WaitForState(() => !cut.Markup.Contains("Cargando"), TimeSpan.FromSeconds(8));

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Single(btn => btn.GetAttribute("class")?.Contains("hs-cta-secondary") == true)
                .Click(), TimeSpan.FromSeconds(10));

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(duplicateCall).Should().Be(1);

            // Rule 2 (ux-ui-good-apps): the user lands on their own context,
            // with the copied exercises ready to edit, not on a catalog.
            navigationManager.Uri.Should().EndWith("/training/today");
            cut.Markup.Should().Contain("Bike");
            cut.Markup.Should().Contain("Añadir ejercicio");
            cut.Markup.Should().NotContain("hs-cta-secondary");

            Services.GetRequiredService<ISnackbar>()
                .ShownSnackbars
                .Should().Contain(snackbar => snackbar.Message == "Entreno anterior copiado a hoy.");
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
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=14")
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
        cut.WaitForState(() => !cut.Markup.Contains("Cargando"), TimeSpan.FromSeconds(8));

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Single(btn => btn.GetAttribute("class")?.Contains("hs-cta-secondary") == true)
                .Click(), TimeSpan.FromSeconds(10));

        cut.WaitForAssertion(() =>
        {
            navigationManager.Uri.Should().EndWith("/training/today");
            cut.Markup.Should().Contain("No se pudo repetir el entreno.");
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
            cut.Markup.Should().Contain("Sin ajustes");
            cut.Markup.Should().NotContain("0 × 0");
            cut.Find("a[aria-label='Editar Bench Press']");
        });
    }

    [Fact]
    public void Quick_Start_Flag_Opens_The_Session_Without_Any_Tap()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts*")
            .Respond("application/json", "[]");
        var startCall = MockHttp.When(HttpMethod.Post, "/Workouts/today")
            .Respond("application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [],
                    "blocks": [],
                    "recentExerciseTypeIds": []
                }
                """);

        // The shell FAB lands here (MainLayout quick start): Hoy must open the
        // session by itself, not show the activation CTA and wait for a tap.
        Services.GetRequiredService<FakeNavigationManager>()
            .NavigateTo("/training/today?start=1");

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() => MockHttp.GetMatchCount(startCall).Should().Be(1), TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(
            () => cut.FindAll(".hs-today-block-add").Should().HaveCount(1),
            TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(
            () => Services.GetRequiredService<FakeNavigationManager>().Uri.Should().NotContain("start=1"),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Today_Renders_The_Summary_When_Finish_Comes_From_The_Shell_Fab()
    {
        // t_745b4960 regression: the FAB finish is an event owned by MainLayout
        // (ActiveSession.FinishAsync), so only the layout re-renders by default.
        // The page must render its own summary after the dialog closes, otherwise
        // the DOM stays frozen on the log view and the user never sees the result.
        var workoutId = Guid.NewGuid();

        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", "[]");
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=14")
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

        Services.AddSingleton<IDialogService, ImmediateDialogService>();

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(
            () => cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Empezar entreno")),
            TimeSpan.FromSeconds(10));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Empezar entreno"))
            .Click();
        cut.WaitForAssertion(
            () => MockHttp.GetMatchCount(startCall).Should().Be(1),
            TimeSpan.FromSeconds(10));
        // The POST continuation hops to the thread pool; wait until the log
        // view is actually rendered, otherwise FinishAsync races StartWorkout.
        cut.WaitForAssertion(
            () => cut.FindAll(".hs-today-timer").Should().NotBeEmpty(),
            TimeSpan.FromSeconds(10));

        // Shell FAB path: the click belongs to MainLayout, not to the page.
        var active = Services.GetRequiredService<ActiveSessionState>();
        active.FinishAsync().GetAwaiter().GetResult();

        cut.WaitForAssertion(
            () => cut.FindAll(".hs-summary-hero").Should().HaveCount(1),
            TimeSpan.FromSeconds(10));
        cut.FindAll("button")
            .Should().Contain(button => button.TextContent.Contains("Volver a Hoy"));
        active.SessionId.Should().BeNull();
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



    // MudBlazor dialogs do not render in bUnit (MudDialogProvider limitation),
    // so the shell-FAB regression test stubs the dialog: the finish handler
    // proceeds as if the user confirmed without fatigue data.
    private sealed class ImmediateDialogService : IDialogService
    {
        public event Action<IDialogReference> OnDialogInstanceAdded { add { } remove { } }
        public event Action<IDialogReference, DialogResult> OnDialogCloseRequested { add { } remove { } }

        public IDialogReference Show<T>() where T : IComponent => Show(typeof(T), string.Empty, new DialogParameters(), new DialogOptions());
        public IDialogReference Show<T>(string title) where T : IComponent => Show(typeof(T), title, new DialogParameters(), new DialogOptions());
        public IDialogReference Show<T>(string title, DialogOptions options) where T : IComponent => Show(typeof(T), title, new DialogParameters(), options);
        public IDialogReference Show<T>(string title, DialogParameters parameters) where T : IComponent => Show(typeof(T), title, parameters, new DialogOptions());
        public IDialogReference Show<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent => Show(typeof(T), title, parameters, options);
        public IDialogReference Show(Type contentComponent) => Show(contentComponent, string.Empty, new DialogParameters(), new DialogOptions());
        public IDialogReference Show(Type contentComponent, string title) => Show(contentComponent, title, new DialogParameters(), new DialogOptions());
        public IDialogReference Show(Type contentComponent, string title, DialogOptions options) => Show(contentComponent, title, new DialogParameters(), options);
        public IDialogReference Show(Type contentComponent, string title, DialogParameters parameters) => Show(contentComponent, title, parameters, new DialogOptions());
        public IDialogReference Show(Type contentComponent, string title, DialogParameters parameters, DialogOptions options) => new ImmediateDialogReference();

        public Task<IDialogReference> ShowAsync<T>() where T : IComponent => Task.FromResult(Show<T>());
        public Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent => Task.FromResult(Show<T>(title));
        public Task<IDialogReference> ShowAsync<T>(string title, DialogOptions options) where T : IComponent => Task.FromResult(Show<T>(title, options));
        public Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters) where T : IComponent => Task.FromResult(Show<T>(title, parameters));
        public Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent => Task.FromResult(Show<T>(title, parameters, options));
        public Task<IDialogReference> ShowAsync(Type contentComponent) => Task.FromResult(Show(contentComponent));
        public Task<IDialogReference> ShowAsync(Type contentComponent, string title) => Task.FromResult(Show(contentComponent, title));
        public Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogOptions options) => Task.FromResult(Show(contentComponent, title, options));
        public Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters) => Task.FromResult(Show(contentComponent, title, parameters));
        public Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters, DialogOptions options) => Task.FromResult(Show(contentComponent, title, parameters, options));

        public IDialogReference CreateReference() => new ImmediateDialogReference();
        public Task<bool?> ShowMessageBox(string title, string message, string yesText = "OK", string noText = "Cancel", string cancelText = "Cancel", DialogOptions options = null) => Task.FromResult<bool?>(true);
        public Task<bool?> ShowMessageBox(string title, Microsoft.AspNetCore.Components.MarkupString message, string yesText = "OK", string noText = "Cancel", string cancelText = "Cancel", DialogOptions options = null) => Task.FromResult<bool?>(true);
        public Task<bool?> ShowMessageBox(MessageBoxOptions messageBoxOptions, DialogOptions options = null) => Task.FromResult<bool?>(true);
        public void Close(DialogReference dialog) { }
        public void Close(DialogReference dialog, DialogResult result) { }
    }

    private sealed class ImmediateDialogReference : IDialogReference
    {
        public Guid Id { get; } = Guid.NewGuid();
        public RenderFragment RenderFragment { get; set; }
        public bool AreParametersRendered { get; set; }
        public Task<DialogResult> Result { get; } = Task.FromResult(DialogResult.Ok<object>(null));
        public TaskCompletionSource<bool> RenderCompleteTaskCompletionSource { get; } = new TaskCompletionSource<bool>();
        public object Dialog { get; } = null;

        public ImmediateDialogReference() => RenderCompleteTaskCompletionSource.TrySetResult(true);

        public void Close() => Result.GetType();
        public void Close(DialogResult result) { }
        public bool Dismiss(DialogResult result) => true;
        public void InjectRenderFragment(RenderFragment rf) { }
        public void InjectDialog(object inst) { }
        public Task<T> GetReturnValueAsync<T>() => Result.ContinueWith(r => r.Result.Data is T typed ? typed : default);
    }

}
