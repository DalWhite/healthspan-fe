using Bunit;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ExerciseCreationFormTests : BlazorTestBase
{
    [Fact]
    public void Requires_Exercise_Selection_Before_Submit()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 21, Name = "Bench Press" }
            })
            .Add(component => component.InitialDate, DateTime.Today));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Choose an exercise before saving.");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Submits_Once_On_Success_And_Resets_The_Form()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When("/workouts")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "type": "default",
                        "date": "{{DateTime.Today:O}}"
                    }
                ]
            """);

        var createExerciseRequest = MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", "{}");

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 21, Name = "Bench Press", WeightType = ExerciseWeightType.Standard }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
        }, timeout: TimeSpan.FromSeconds(3));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(createExerciseRequest).Should().Be(1);
            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().NotContain("The exercise was not saved.");
            cut.Markup.Should().NotContain("We couldn't save the exercise right now.");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Server_Failure_Shows_One_Error_And_Does_Not_Report_Success()
    {
        var workoutId = Guid.NewGuid();
        var callbackCount = 0;

        MockHttp.When("/workouts")
            .Respond("application/json", $$"""
                [
                    {
                        "id": "{{workoutId}}",
                        "type": "default",
                        "date": "{{DateTime.Today:O}}"
                    }
                ]
            """);

        var createExerciseRequest = MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 21, Name = "Bench Press", WeightType = ExerciseWeightType.Standard }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.OnExerciseAdded, _ => callbackCount++));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(createExerciseRequest).Should().Be(1);
            cut.FindAll(".mud-alert").Should().ContainSingle();
            cut.Markup.Should().Contain("The exercise could not be saved");
            callbackCount.Should().Be(0);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void New_Workout_And_Exercise_Produce_No_Intermediate_Notification()
    {
        var workoutId = Guid.NewGuid();
        var callbackCount = 0;
        var snackbar = Services.GetRequiredService<ISnackbar>();

        MockHttp.When(HttpMethod.Get, "/workouts")
            .Respond("application/json", "[]");

        MockHttp.When(HttpMethod.Post, "/Workouts")
            .Respond(HttpStatusCode.Created, "application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "type": "default",
                    "date": "{{DateTime.Today:O}}"
                }
            """);

        MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", $$"""{ "id": "{{Guid.NewGuid()}}" }""");

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 21, Name = "Bench Press", WeightType = ExerciseWeightType.Standard }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.OnExerciseAdded, _ => callbackCount++));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            callbackCount.Should().Be(1);
            snackbar.ShownSnackbars.Should().BeEmpty();
            cut.FindAll(".mud-alert").Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Session_Mode_Uses_Nested_Workout_Endpoint_And_Returns_Summary()
    {
        var workoutId = Guid.NewGuid();
        WorkoutSessionResource? updatedSession = null;

        var addRequest = MockHttp
            .When(HttpMethod.Post, $"/Workouts/{workoutId}/exercises")
            .Respond(HttpStatusCode.OK, "application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "{{DateTime.Today:yyyy-MM-dd}}",
                    "type": "Training",
                    "exercises": [
                        {
                            "id": "{{Guid.NewGuid()}}",
                            "typeId": 21,
                            "name": "Bench Press",
                            "series": 1,
                            "repetitions": 8,
                            "weight": 0
                        }
                    ],
                    "recentExerciseTypeIds": [21]
                }
                """);

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    IsFavourite = true,
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.WorkoutId, workoutId)
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.ShowDatePicker, false)
            .Add(component => component.ShowQuickPicks, true)
            .Add(component => component.UseWorkoutSessionEndpoint, true)
            .Add(component => component.OnSessionUpdated, session => updatedSession = session));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(addRequest).Should().Be(1);
            updatedSession.Should().NotBeNull();
            updatedSession!.Exercises.Should().ContainSingle();
            cut.Markup.Should().Contain("Favoritos");
        });
    }

    [Fact]
    public void Custom_Training_Tags_Are_Submitted_With_The_Exercise()
    {
        var workoutId = Guid.NewGuid();
        AddExerciseResource? submitted = null;

        MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", "{}");

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.WorkoutId, workoutId)
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.OnExerciseAdded, exercise => submitted = exercise));

        // Tags live behind an explicit affordance: the fast path hides them so
        // the common add stays short.
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Añadir etiquetas"))
            .Click();

        cut.Find("input[placeholder='Bilbo']")
            .Input("Bilbo, Tempo");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            submitted.Should().NotBeNull();
            submitted!.Tags.Should().Equal("Bilbo", "Tempo");
        });
    }

    [Fact]
    public void Sets_And_Reps_Are_Prefilled_When_An_Exercise_Is_Chosen()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.WorkoutId, Guid.NewGuid()));

        // The fast path never asks for numbers first: they arrive prefilled and
        // stay editable.
        cut.WaitForAssertion(() =>
        {
            NumericInput(cut, 0)
                .GetAttribute("value").Should().Be("3");
            NumericInput(cut, 1)
                .GetAttribute("value").Should().Be("10");
        });
    }

    [Fact]
    public void Sets_And_Reps_Default_To_What_Was_Logged_For_The_Same_Exercise()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.WorkoutId, Guid.NewGuid())
            .Add(component => component.PrescriptionHints, new[]
            {
                new WorkoutSessionExerciseResource
                {
                    Id = Guid.NewGuid(),
                    TypeId = 21,
                    Name = "Bench Press",
                    Series = 4,
                    Repetitions = 8
                }
            }));

        cut.WaitForAssertion(() =>
        {
            NumericInput(cut, 0)
                .GetAttribute("value").Should().Be("4");
            NumericInput(cut, 1)
                .GetAttribute("value").Should().Be("8");
        });
    }

    [Fact]
    public void Edited_Sets_Are_Not_Overwritten_By_The_Default()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    IsFavourite = true,
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.ShowQuickPicks, true)
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.WorkoutId, Guid.NewGuid())
            .Add(component => component.PrescriptionHints, new[]
            {
                new WorkoutSessionExerciseResource
                {
                    Id = Guid.NewGuid(),
                    TypeId = 21,
                    Name = "Bench Press",
                    Series = 4,
                    Repetitions = 8
                }
            }));

        cut.WaitForAssertion(() => NumericInput(cut, 0)
            .GetAttribute("value").Should().Be("4"));

        NumericInput(cut, 0).Change("6");

        // Re-picking the same exercise must not wipe what the user just typed.
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Bench Press"))
            .Click();

        cut.WaitForAssertion(() => NumericInput(cut, 0)
            .GetAttribute("value").Should().Be("6"));
    }

    [Fact]
    public void Logged_Sets_Become_The_Default_For_The_Next_Entry()
    {
        var workoutId = Guid.NewGuid();
        var submissions = new List<AddExerciseResource>();

        MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", "{}");

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.WorkoutId, workoutId)
            .Add(component => component.OnExerciseAdded,
                (Action<AddExerciseResource>)(exercise => submissions.Add(exercise))));

        cut.WaitForAssertion(() => NumericInput(cut, 0)
            .GetAttribute("value").Should().Be("3"));

        NumericInput(cut, 0).Change("5");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            submissions.Should().HaveCount(1);
            submissions[0].Series.Should().Be(5);
        });

        // The form lands back ready for the next entry, carrying the numbers
        // the user just logged.
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            submissions.Should().HaveCount(2);
            submissions[1].Series.Should().Be(5);
            submissions[1].Repetitions.Should().Be(10);
        });
    }

    [Fact]
    public void Saves_The_Exercise_When_Sets_And_Reps_Are_Cleared()
    {
        var workoutId = Guid.NewGuid();
        AddExerciseResource? submitted = null;

        var createExerciseRequest = MockHttp
            .When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", "{}");

        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource
                {
                    Id = 21,
                    Name = "Bench Press",
                    WeightType = ExerciseWeightType.Standard
                }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today)
            .Add(component => component.WorkoutId, workoutId)
            .Add(component => component.OnExerciseAdded,
                (Action<AddExerciseResource>)(exercise => submitted = exercise)));

        // The fast path prefills 3x10; the user can wipe it to log the exercise
        // now and fill the numbers later.
        cut.WaitForAssertion(() => NumericInput(cut, 0)
            .GetAttribute("value").Should().Be("3"));

        NumericInput(cut, 0).Change("");
        NumericInput(cut, 1).Change("");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(createExerciseRequest).Should().Be(1);
            submitted.Should().NotBeNull();
            submitted!.Series.Should().BeNull();
            submitted.Repetitions.Should().BeNull();
            cut.FindAll(".mud-alert").Should().BeEmpty();
        }, timeout: TimeSpan.FromSeconds(3));
    }

    // MudBlazor gives these inputs unstable ids, so the prescription grid is
    // addressed by position: Sets, Reps, Weight.
    private static AngleSharp.Dom.IElement NumericInput(
        Bunit.IRenderedFragment cut,
        int index)
        => cut.FindAll("input[inputmode='numeric']")[index];
}
