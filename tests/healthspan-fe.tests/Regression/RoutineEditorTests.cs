using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components.Routines;
using healthspan_fe_wasm_pwa.Pages.Routines;
using healthspan_fe_wasm_pwa.Pages.Trainees;
using RichardSzalay.MockHttp;
using System.Net;
using System.Text;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Regression coverage for the trainer routine editor: the shared editor
/// component (edit + read modes) and the trainee read-only routine view.
/// </summary>
public class RoutineEditorTests : BlazorTestBase
{
    private static readonly Guid RoutineId =
        Guid.Parse("00000000-0000-0000-0000-000000000abc");
    private const string TraineeId = "trainee-1";

    private static RoutineResource AssignedRoutine(string? trainerId) => new()
    {
        Id = RoutineId,
        Name = "Push plan",
        Description = "Upper body push work",
        UserId = TraineeId,
        TrainerId = trainerId,
        Version = 1,
        WorkoutTemplates =
        [
            new()
            {
                Id = 1,
                Type = "Push",
                FrequencyPerWeek = 2,
                Exercises =
                [
                    new()
                    {
                        Id = 1,
                        ExerciseTypeId = 1,
                        ExerciseTypeName = "Bench Press",
                        Order = 1,
                        Sets = 4,
                        Reps = 8,
                        LoadKg = 60,
                        RestSeconds = 90,
                        Notes = "Control the descent, 2 seconds down."
                    }
                ]
            }
        ]
    };

    private static List<GetExerciseTypeResource> ExerciseTypes() =>
    [
        new() { Id = 1, Name = "Bench Press" },
        new() { Id = 2, Name = "Incline Bench Press" }
    ];

    [Fact]
    public void Editor_Shows_Compact_Targets_And_The_Trainer_Explanation()
    {
        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, AssignedRoutine(trainerId: "coach-1"))
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes()));

        var summary = cut.Find(".hs-routine-exercise-targets").TextContent;
        summary.Should().Contain("4 × 8");
        summary.Should().Contain("60 kg");
        summary.Should().Contain("90s rest");

        // Targets are collapsed by default; the explanation is always visible.
        cut.FindAll(".hs-routine-exercise-fields").Should().BeEmpty();
        cut.Find(".hs-routine-exercise-note").TextContent
            .Should().Contain("Control the descent");
    }

    [Fact]
    public void Editor_Edit_Toggle_Reveals_Targets_And_Explanation_Fields()
    {
        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, AssignedRoutine(trainerId: "coach-1"))
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes()));

        cut.Find("button[aria-label='Edit Bench Press']").Click();

        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-routine-exercise-fields").Count.Should().Be(1));
    }

    [Fact]
    public void Editor_Save_Posts_To_Routines_And_Raises_OnSaved()
    {
        var saved = AssignedRoutine(trainerId: "coach-1");
        saved.Version = 2;

        MockHttp.When(HttpMethod.Post, "/routines")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(saved));

        RoutineResource? raised = null;
        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, null)
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes())
            .Add(p => p.OnSaved, (RoutineResource value) => raised = value));

        // Give the new routine a name, then save.
        cut.Find(".hs-routine-editor-head input").Input("Push plan");
        cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Save routine")).Click();

        cut.WaitForAssertion(() => raised.Should().NotBeNull(), TimeSpan.FromSeconds(5));
        raised!.Name.Should().Be("Push plan");
        raised.UserId.Should().Be(TraineeId);
    }

    [Fact]
    public void Editor_Save_Sends_If_Match_For_Existing_Routine()
    {
        var existing = AssignedRoutine(trainerId: "coach-1");
        var saved = AssignedRoutine(trainerId: "coach-1");
        saved.Version = 2;

        var captured = new HttpRequestMessage[1];
        MockHttp.When(HttpMethod.Put, $"/routines/{RoutineId}")
            .Respond(request =>
            {
                captured[0] = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(saved),
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, existing)
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes()));

        cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Save routine")).Click();

        cut.WaitForAssertion(() => captured[0].Should().NotBeNull(), TimeSpan.FromSeconds(5));
        captured[0]!.Headers.GetValues("If-Match").Single().Should().Be("W/\"v1\"");
    }

    [Fact]
    public void Editor_Read_Only_Hides_Edit_Actions_But_Keeps_Explanation()
    {
        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, AssignedRoutine(trainerId: "coach-1"))
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes())
            .Add(p => p.ReadOnly, true));

        cut.FindAll("button[aria-label^='Edit ']").Should().BeEmpty();
        cut.FindAll("button").Should().NotContain(button =>
            button.TextContent.Contains("Save routine"));
        cut.Find(".hs-routine-exercise-note").TextContent
            .Should().Contain("Control the descent");
    }

    [Fact]
    public void MyRoutines_Labels_A_Routine_Assigned_By_A_Coach()
    {
        MockHttp.When(HttpMethod.Get, "/routines")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(
                new[] { AssignedRoutine(trainerId: "coach-1") }));

        var cut = RenderComponent<MyRoutines>();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Assigned by your coach"), TimeSpan.FromSeconds(5));
        cut.Find(".hs-routine-exercise-note").TextContent
            .Should().Contain("Control the descent");
    }

    [Fact]
    public void TraineeRoutineEditor_Loads_The_Routine_Scoped_To_The_Trainee()
    {
        // The routine belongs to the trainee, so the trainer's GET must carry
        // traineeId: without it the API resolves the routine against the trainer
        // and answers 404, which made the "Edit" button unopenable.
        var routineRequests = new List<string>();

        MockHttp.When(HttpMethod.Get, $"/users/{TraineeId}")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(
                new UserResource { Id = Guid.Parse("00000000-0000-0000-0000-0000000000aa"), Name = "Ana" }));
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(ExerciseTypes()));
        MockHttp.When(HttpMethod.Get, $"/routines/{RoutineId}")
            .Respond(request =>
            {
                routineRequests.Add(request.RequestUri!.ToString());
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(AssignedRoutine(trainerId: "coach-1")),
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var cut = RenderComponent<TraineeRoutineEditor>(parameters => parameters
            .Add(p => p.TraineeId, TraineeId)
            .Add(p => p.RoutineId, RoutineId));

        cut.WaitForAssertion(() => routineRequests.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));

        var requested = routineRequests.Single();
        requested.Should().Contain("traineeId=").And.Contain(TraineeId);

        cut.Markup.Should().NotContain("could not be loaded");
        cut.Find(".hs-routine-exercise-note").TextContent
            .Should().Contain("Control the descent");
    }

    [Fact]
    public void Editor_Keeps_In_Progress_Edits_When_The_Parent_Rerenders()
    {
        var saved = AssignedRoutine(trainerId: "coach-1");
        saved.Version = 2;
        var webJson = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        RoutineResource? payload = null;

        MockHttp.When(HttpMethod.Put, $"/routines/{RoutineId}")
            .Respond(request =>
            {
                payload = System.Text.Json.JsonSerializer.Deserialize<RoutineResource>(
                    request.Content!.ReadAsStringAsync().Result, webJson);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(saved, webJson),
                        Encoding.UTF8,
                        "application/json")
                };
            });

        var cut = RenderComponent<RoutineEditor>(parameters => parameters
            .Add(p => p.Initial, AssignedRoutine(trainerId: "coach-1"))
            .Add(p => p.TargetUserId, TraineeId)
            .Add(p => p.ExerciseTypes, ExerciseTypes()));

        cut.Find("button[aria-label='Edit Bench Press']").Click();
        cut.WaitForAssertion(() =>
            cut.FindAll(".hs-routine-exercise-fields").Count.Should().Be(1));

        cut.Find(".hs-routine-editor-head input").Input("Push plan v2");

        // A parent re-render (new parameter instance, same Initial reference) must not
        // rebuild the model and throw away what the trainer is typing.
        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.ExerciseTypes, ExerciseTypes()));

        cut.FindAll(".hs-routine-exercise-fields").Count.Should().Be(1);

        cut.FindAll("button").Single(button =>
            button.TextContent.Contains("Save routine")).Click();

        cut.WaitForAssertion(() => payload.Should().NotBeNull(), TimeSpan.FromSeconds(5));
        payload!.Name.Should().Be("Push plan v2");
    }
}
