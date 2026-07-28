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
            cut.Markup.Should().Contain("Favourites");
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

        cut.Find("input[placeholder='Bilbo']")
            .Input("Bilbo, Tempo");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            submitted.Should().NotBeNull();
            submitted!.Tags.Should().Equal("Bilbo", "Tempo");
        });
    }
}
