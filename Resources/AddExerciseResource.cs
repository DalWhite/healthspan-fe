public record AddExerciseResource
{
    public Guid? WorkoutId { get; set; }
    public int? TypeId { get; set; }
    public int Series { get; set; }
    public int Repetitions { get; set; }
    public int Weight { get; set; }
}