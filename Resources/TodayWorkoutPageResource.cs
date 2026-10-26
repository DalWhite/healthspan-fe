// Aggregate payload for the whole Today screen (t_101e2b0a): one round-trip
// replaces /Workouts/today/sessions + /workouts?page=1&pageSize=14 +
// /Workouts/{id} x2 + /HealthProfile. Mirrors the API contract of the same name.
public sealed class TodayWorkoutPageResource
{
    public List<WorkoutSessionResource> Sessions { get; set; } = [];
    public List<TodayRecentWorkoutResource> RecentWorkouts { get; set; } = [];

    /// <summary>Exercise type id -> load logged in the two most recent sessions.</summary>
    public Dictionary<int, WorkoutHistoryExerciseResource> PreviousLoads { get; set; } = [];

    public decimal? ProfileWeightKg { get; set; }
}

public sealed class TodayRecentWorkoutResource
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public int ExerciseCount { get; set; }
    public string? Modality { get; set; }
    public string? BlockLabel { get; set; }
    public WorkoutHistoryExerciseResource? TopExercise { get; set; }
    public int Attempts { get; set; }
    public int CompletedAttempts { get; set; }
}
