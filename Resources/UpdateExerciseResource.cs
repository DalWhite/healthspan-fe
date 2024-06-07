public class UpdateExerciseResource
{
    public Guid? Id { get; set; }
    public int? Series { get; set; }
    public int? Repetitions { get; set; }
    public double? Weight { get; set; }
    public GetExerciseTypeResource? Type { get; set; }
    public GetWorkoutResource? Workout { get; set; }
}