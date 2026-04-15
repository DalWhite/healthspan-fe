public class GetExerciseResource
{
    public Guid? Id { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public decimal? Weight { get; set; }
    public double? MaxRepetition { get; set; }
    public int? Volume { get; set; }
    public int? Intensity { get; set; }
    public decimal? BodyWeight { get; set; }
    public decimal? AddedWeight { get; set; }
    public ExerciseWeightType? WeightType { get; set; }
    public GetExerciseTypeResource? Type { get; set; }
    public GetWorkoutResource? Workout { get; set; }
}