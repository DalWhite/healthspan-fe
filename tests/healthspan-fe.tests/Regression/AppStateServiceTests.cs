using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Unit tests for the static search/filter helpers in AppStateService
/// (no HTTP needed — these are pure functions).
/// </summary>
public class AppStateServiceTests
{
    [Theory]
    [InlineData("Push", "push", true)]
    [InlineData("Push", "PUSH", true)]
    [InlineData("Push", "legs", false)]
    [InlineData("Push", "", true)]
    [InlineData("Push", null, true)]
    public void MatchesSearch_Workout_Filters_By_Type(string workoutType, string? query, bool expected)
    {
        var workout = new GetWorkoutResource
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Type = workoutType
        };

        AppStateService.MatchesSearch(workout, query ?? "").Should().Be(expected);
    }

    [Theory]
    [InlineData("Bench Press", "bench", true)]
    [InlineData("Bench Press", "squat", false)]
    [InlineData("Bench Press", "", true)]
    public void MatchesSearch_ExerciseType_Filters_By_Name(string name, string query, bool expected)
    {
        var exerciseType = new GetExerciseTypeResource { Id = 1, Name = name };

        AppStateService.MatchesSearch(exerciseType, query).Should().Be(expected);
    }

    [Theory]
    [InlineData("Bench Press", "press", true)]
    [InlineData("Bench Press", "deadlift", false)]
    [InlineData("Bench Press", "", true)]
    public void MatchesSearch_Exercise_Filters_By_TypeName(string typeName, string query, bool expected)
    {
        var exercise = new GetExerciseResource
        {
            Id = Guid.NewGuid(),
            Type = new GetExerciseTypeResource { Name = typeName }
        };

        AppStateService.MatchesSearch(exercise, query).Should().Be(expected);
    }
}
