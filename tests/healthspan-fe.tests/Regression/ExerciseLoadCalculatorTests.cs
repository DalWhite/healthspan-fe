using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ExerciseLoadCalculatorTests
{
    [Fact]
    public void NormalizeForSubmission_Bodyweight_Uses_Total_Load()
    {
        var exercise = new AddExerciseResource
        {
            WeightType = ExerciseWeightType.BodyWeight,
            BodyWeight = 82m,
            AddedWeight = 10m
        };

        ExerciseLoadCalculator.NormalizeForSubmission(exercise);

        exercise.Weight.Should().Be(92m);
    }
}
