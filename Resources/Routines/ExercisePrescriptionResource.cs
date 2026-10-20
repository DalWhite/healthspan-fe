public class ExercisePrescriptionResource
{
    public int Id { get; set; }
    public int ExerciseTypeId { get; set; }
    public string? ExerciseTypeName { get; set; }
    public int Order { get; set; }
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public double? LoadKg { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }
}
