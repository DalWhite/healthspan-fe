public sealed class WorkoutSessionResource
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string? Type { get; set; }
    public List<WorkoutSessionExerciseResource> Exercises { get; set; } = new();
    public List<int> RecentExerciseTypeIds { get; set; } = new();

    // Universal tracker: intent chosen in two taps, blocks optional, fatigue
    // only asked when the session is closed.
    public string? Goal { get; set; }
    public List<string> SecondaryGoals { get; set; } = [];
    public string? Modality { get; set; }
    public int? PerceivedFatigue { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkoutBlockResource> Blocks { get; set; } = [];
}

public sealed class WorkoutSessionExerciseResource
{
    public Guid Id { get; set; }
    public int TypeId { get; set; }
    public string? Name { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyWeight { get; set; }
    public decimal? AddedWeight { get; set; }
    public string? Comments { get; set; }
    public List<string> Tags { get; set; } = new();

    // Block + attempt tracking. BlockId is null for exercises in the implicit
    // Normal block, so old sessions render exactly as before. Nullable because
    // the API omits them for legacy rows and null must not fail deserialisation.
    public Guid? BlockId { get; set; }
    public int? Order { get; set; }
    public int? Attempts { get; set; }
    public int? CompletedAttempts { get; set; }
    public string? QualityIntent { get; set; }
    public string? LoadType { get; set; }
    public List<string> Adaptations { get; set; } = [];
}

public sealed class StartWorkoutSessionResource
{
    public DateOnly Date { get; set; }
    public string Type { get; set; } = "Training";
    public bool CreateNew { get; set; }
}
