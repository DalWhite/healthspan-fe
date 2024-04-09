public record AddExerciseResource
{
    public Guid? WorkoutId { get; set; }
    public int? TypeId { get; set; }
    public int Series { get; set; } = 1;
    public int Repetitions { get; set; } = 8;
    public decimal Weight { get; set; } = 0;
}