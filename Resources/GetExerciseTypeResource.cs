using System.Collections.Generic;

public record GetExerciseTypeResource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageRoute { get; set; }
    public List<ExerciseTypeMuscleAssignmentResource> MuscleAssignments { get; set; } = new();
    public bool IsFavourite { get; set; }
    public ExerciseWeightType? WeightType { get; set; } = ExerciseWeightType.Standard;
    public int? ParentExerciseTypeId { get; set; }
    public List<ExerciseTag>? Tags { get; set; } = new List<ExerciseTag>();
    public List<GetExerciseTypeResource> Variations { get; set; } = new();
}
