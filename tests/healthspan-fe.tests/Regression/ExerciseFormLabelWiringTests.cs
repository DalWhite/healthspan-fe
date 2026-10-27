using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WCAG 1.3.1/3.3.2 regression (a11y run7 / card t_7a326722): the exercise
/// logging form on /training/today must name every input through
/// label[for] -> input[id]. MudBlazor 6.21 wires this when an explicit `id`
/// is splatted onto the input component (FieldId -> label `for`, id -> the
/// rendered &lt;input&gt;). Before the fix the label's `for` pointed at a
/// generated "mudinput-*" id that never reached the DOM, so the visible
/// labels (Ejercicio, Series, Reps, Peso) named nothing.
/// </summary>
public class ExerciseFormLabelWiringTests : BlazorTestBase
{
    private static void AssertLabelWired(IRenderedFragment cut, string id, string expectedLabel)
    {
        var input = cut.Find($"input#{id}");
        input.Should().NotBeNull($"input#{id} must exist (id must land on the <input>)");

        var label = cut.Find($"label[for=\"{id}\"]");
        label.TextContent.Trim().Should().Be(expectedLabel,
            $"label[for=\"{id}\"] must be the visible label for that input");
    }

    [Fact]
    public void Standard_Exercise_Inputs_Are_LabelWired()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 21, Name = "Bench Press", WeightType = ExerciseWeightType.Standard }
            })
            .Add(component => component.InitialExerciseTypeId, 21)
            .Add(component => component.InitialDate, DateTime.Today));

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("input#exercise-series").Should().HaveCount(1);
        }, timeout: TimeSpan.FromSeconds(3));

        AssertLabelWired(cut, "exercise-type", "Ejercicio");
        AssertLabelWired(cut, "exercise-series", "Series");
        AssertLabelWired(cut, "exercise-reps", "Reps");
        AssertLabelWired(cut, "exercise-weight", "Peso (kg)");
    }

    [Fact]
    public void BodyWeight_Exercise_Inputs_Are_LabelWired()
    {
        var cut = RenderComponent<ExerciseCreationForm>(parameters => parameters
            .Add(component => component.ExerciseTypes, new[]
            {
                new GetExerciseTypeResource { Id = 22, Name = "Pull Up", WeightType = ExerciseWeightType.BodyWeight }
            })
            .Add(component => component.InitialExerciseTypeId, 22)
            .Add(component => component.InitialDate, DateTime.Today));

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("input#exercise-body-weight").Should().HaveCount(1);
        }, timeout: TimeSpan.FromSeconds(3));

        AssertLabelWired(cut, "exercise-type", "Ejercicio");
        AssertLabelWired(cut, "exercise-series", "Series");
        AssertLabelWired(cut, "exercise-reps", "Reps");
        AssertLabelWired(cut, "exercise-body-weight", "Peso corporal (kg)");
        AssertLabelWired(cut, "exercise-added-weight", "Peso añadido (kg)");
    }
}
