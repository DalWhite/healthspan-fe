using System.Collections.Generic;

public class AddExerciseTypeResource
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageRoute { get; set; }
    public List<ExerciseTypeMuscleAssignmentResource> MuscleAssignments { get; set; } = new();
    public int? ParentExerciseTypeId { get; set; }
    public ExerciseWeightType? WeightType { get; set; } = ExerciseWeightType.Standard;
    public List<ExerciseTag>? Tags { get; set; } = new List<ExerciseTag>();
}
