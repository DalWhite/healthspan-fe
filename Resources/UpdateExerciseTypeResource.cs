using System.Collections.Generic;

public record UpdateExerciseTypeResource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageRoute { get; set; }
    public int? PrimaryMuscleId { get; set; }
    public int? SecondaryMuscleId { get; set; }
    public bool IsFavourite { get; set; }
    public List<ExerciseTag>? Tags { get; set; } = new List<ExerciseTag>();
}