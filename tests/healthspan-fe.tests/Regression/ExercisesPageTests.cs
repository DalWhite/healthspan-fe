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
            .Respond(HttpStatusCode.BadRequest, "application/problem+json", "{}");

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo($"https://localhost/exercises/edit/{exerciseId}");

        var cut = RenderComponent<EditExercise>(parameters => parameters
            .Add(component => component.ExerciseId, exerciseId.ToString()));

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Should().Contain(button => button.TextContent.Contains("Delete")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Delete")).Click();

        cut.WaitForAssertion(() =>
            navigationManager.Uri.Should().EndWith($"/exercises/edit/{exerciseId}"),
            timeout: TimeSpan.FromSeconds(3));
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
