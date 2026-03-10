public class GetExerciseResource
{
    public Guid? Id { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public double? Weight { get; set; }
    public double? MaxRepetition { get; set; }
    public int? Volume { get; set; }
    public double? Intensity { get; set; }
    public double? BodyWeight { get; set; }
    public double? AddedWeight { get; set; }
    public ExerciseWeightType? WeightType { get; set; }
    public GetExerciseTypeResource? Type { get; set; }
    public GetWorkoutResource? Workout { get; set; }
}