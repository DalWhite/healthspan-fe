using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ExercisesPageTests : BlazorTestBase
{
    [Fact]
    public void Renders_When_ExerciseTypeNames_Are_Duplicated()
    {
        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    { "id": 21, "name": "Face pull" },
                    { "id": 34, "name": "Face pull" }
                ]
            """);

        MockHttp.When("/api/muscles")
            .Respond("application/json", "[]");

        MockHttp.When("/Exercises*")
            .Respond("application/json", """
                {
                    "items": [],
                    "totalCount": 0,
                    "pageSize": 20,
                    "currentPage": 1,
                    "totalPages": 0
                }
            """);

        var cut = RenderComponent<Exercises>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No exercises found");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Create_Page_Preselects_Exercise_Type_And_Today_Date_From_Query()
    {
        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    { "id": 21, "name": "Bench Press" },
                    { "id": 34, "name": "Incline Bench Press" }
                ]
            """);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/exercises/create?exerciseTypeId=21");

        var cut = RenderComponent<CreateExercises>();

        cut.WaitForAssertion(() =>
        {
            var creationForm = cut.FindComponent<ExerciseCreationForm>().Instance;
            creationForm.InitialExerciseTypeId.Should().Be(21);
            creationForm.InitialDate?.Date.Should().Be(DateTime.Today);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Create_Page_Submit_Navigates_Back_After_Successful_Save()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    { "id": 21, "name": "Bench Press", "weightType": 0 }
                ]
            """);

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

        MockHttp.When(HttpMethod.Post, "/Exercises")
            .Respond(HttpStatusCode.Created, "application/json", "{}");

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/exercises/create?exerciseTypeId=21");

        var cut = RenderComponent<CreateExercises>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bench Press");
        }, timeout: TimeSpan.FromSeconds(3));

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            navigationManager.Uri.Should().EndWith("/training/history");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Create_Page_Appends_Multiple_Exercises_To_Existing_Workout()
    {
        var workoutId = Guid.NewGuid();

        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    {
                        "id": 21,
                        "name": "Bench Press",
                        "isFavourite": true,
                        "weightType": 0
                    }
                ]
            """);
        var addRequest = MockHttp
            .When(HttpMethod.Post, $"/Workouts/{workoutId}/exercises")
            .Respond(HttpStatusCode.OK, "application/json", $$"""
                {
                    "id": "{{workoutId}}",
                    "date": "2025-06-02",
                    "type": "Training",
                    "exercises": [],
                    "recentExerciseTypeIds": []
                }
            """);

        var navigationManager =
            Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            $"https://localhost/exercises/create?workoutId={workoutId}" +
            "&date=2025-06-02&exerciseTypeId=21" +
            "&returnUrl=%2Ftraining%2Fhistory");

        var cut = RenderComponent<CreateExercises>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Add to workout");
            var form = cut.FindComponent<ExerciseCreationForm>().Instance;
            form.WorkoutId.Should().Be(workoutId);
            form.InitialDate.Should().Be(new DateTime(2025, 6, 2));
            form.ShowDatePicker.Should().BeFalse();
            form.UseWorkoutSessionEndpoint.Should().BeTrue();
        });

        cut.Find("form").Submit();
        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(addRequest).Should().Be(1);
            navigationManager.Uri.Should().Contain("/exercises/create?");
        });

        cut.Find("form").Submit();
        cut.WaitForAssertion(() =>
            MockHttp.GetMatchCount(addRequest).Should().Be(2));

        cut.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Done")
            .Click();
        cut.WaitForAssertion(() =>
            navigationManager.Uri.Should().EndWith("/training/history"));
    }

    [Fact]
    public void Edit_Page_Update_Navigates_Back_After_Successful_Save()
    {
        var exerciseId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        ConfigureEditExerciseRequests(exerciseId, workoutId);

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

        var updateRequest = MockHttp.When(HttpMethod.Put, "/exercises")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo($"https://localhost/exercises/edit/{exerciseId}");

        var cut = RenderComponent<EditExercise>(parameters => parameters
            .Add(component => component.ExerciseId, exerciseId.ToString()));

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Should().Contain(button => button.TextContent.Contains("Save Form")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Save Form")).Click();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(updateRequest).Should().Be(1);
            navigationManager.Uri.Should().EndWith("/training/history");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Edit_Page_Delete_Navigates_Back_Only_After_Success()
    {
        var exerciseId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        ConfigureEditExerciseRequests(exerciseId, workoutId);

        var deleteRequest = MockHttp.When(HttpMethod.Delete, $"/exercises/{exerciseId}")
            .Respond(HttpStatusCode.NoContent);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo($"https://localhost/exercises/edit/{exerciseId}");

        var cut = RenderComponent<EditExercise>(parameters => parameters
            .Add(component => component.ExerciseId, exerciseId.ToString()));

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Should().Contain(button => button.TextContent.Contains("Delete")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Delete")).Click();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(deleteRequest).Should().Be(0);
            cut.Markup.Should().Contain("Delete this entry?");
            cut.Markup.Should().Contain("The workout and exercise type stay available.");
        });
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Delete entry"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            MockHttp.GetMatchCount(deleteRequest).Should().Be(1);
            navigationManager.Uri.Should().EndWith("/training/history");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Edit_Page_Delete_Failure_Stays_On_Edit_Page()
    {
        var exerciseId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        ConfigureEditExerciseRequests(exerciseId, workoutId);

        MockHttp.When(HttpMethod.Delete, $"/exercises/{exerciseId}")
            .Respond(
                HttpStatusCode.Conflict,
                "application/problem+json",
                """
                {
                  "title": "The exercise could not be deleted.",
                  "detail": "Reload the workout and try again."
                }
                """);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo($"https://localhost/exercises/edit/{exerciseId}");

        var cut = RenderComponent<EditExercise>(parameters => parameters
            .Add(component => component.ExerciseId, exerciseId.ToString()));

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Should().Contain(button => button.TextContent.Contains("Delete")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Delete")).Click();
        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Delete entry")));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Delete entry"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            navigationManager.Uri.Should().EndWith($"/exercises/edit/{exerciseId}");
            cut.Markup.Should().Contain("The exercise could not be deleted.");
            cut.Markup.Should().Contain("Reload the workout and try again.");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Edit_Page_Preserves_Validated_ReturnUrl_After_Save()
    {
        var exerciseId = Guid.NewGuid();
        var workoutId = Guid.NewGuid();
        ConfigureEditExerciseRequests(exerciseId, workoutId);

        MockHttp.When(HttpMethod.Put, "/exercises")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            $"https://localhost/training/history/{exerciseId}/edit?returnUrl=%2Ftraining%2Flibrary%2F21%3Fsection%3Dhistory");

        var cut = RenderComponent<EditExercise>(parameters => parameters
            .Add(component => component.ExerciseId, exerciseId.ToString()));

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Save Form")));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Save Form"))
            .Click();

        cut.WaitForAssertion(() =>
            navigationManager.Uri.Should()
                .EndWith("/training/library/21?section=history"));
    }

    private void ConfigureEditExerciseRequests(Guid exerciseId, Guid workoutId)
    {
        MockHttp.When("/ExerciseTypes")
            .Respond("application/json", """
                [
                    { "id": 21, "name": "Bench Press", "weightType": 0 }
                ]
            """);

        MockHttp.When(HttpMethod.Get, $"/exercises/{exerciseId}")
            .Respond("application/json", $$"""
                {
                    "id": "{{exerciseId}}",
                    "series": 3,
                    "repetitions": 8,
                    "weight": 80,
                    "type": { "id": 21, "name": "Bench Press", "weightType": 0 },
                    "workout": {
                        "id": "{{workoutId}}",
                        "type": "default",
                        "date": "{{DateTime.Today:O}}"
                    }
                }
            """);

        MockHttp.When(HttpMethod.Get, $"/exercises/{exerciseId}/metrics")
            .Respond("application/json", "{}");
    }
}
