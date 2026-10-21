public class UpdateExerciseResource
{
    public Guid? Id { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyWeight { get; set; }
    public decimal? AddedWeight { get; set; }
    public ExerciseWeightType? WeightType { get; set; }
    public List<string> Tags { get; set; } = [];
    public GetExerciseTypeResource? Type { get; set; }
    public GetWorkoutResource? Workout { get; set; }

    // Universal tracker: the local mirror accepts these on update (the API
    // applies them through the sync payload), so completing an attempt works
    // offline and drains to the server later.
    public int? Attempts { get; set; }
    public int? CompletedAttempts { get; set; }
    public string? QualityIntent { get; set; }
    public string? LoadType { get; set; }
}
