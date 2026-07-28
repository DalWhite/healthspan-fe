using System.ComponentModel.DataAnnotations;

public record AddExerciseResource : IValidatableObject
{
    public Guid? WorkoutId { get; set; }
    public int? TypeId { get; set; }

    [Range(1, 100, ErrorMessage = "Series must be between 1 and 100")]
    public int Series { get; set; } = 1;

    [Range(0, 1000, ErrorMessage = "Repetitions must be between 0 and 1000")]
    public int Repetitions { get; set; } = 8;

    [Range(0, 10000, ErrorMessage = "Weight must be between 0 and 10,000")]
    public decimal Weight { get; set; } = 0;

    public ExerciseWeightType? WeightType { get; set; } = ExerciseWeightType.Standard;

    // When WeightType == BodyWeight these two fields are used
    [Range(0, 10000, ErrorMessage = "Body weight must be between 0 and 10,000")]
    public decimal? BodyWeight { get; set; }

    [Range(0, 10000, ErrorMessage = "Added weight must be between 0 and 10,000")]
    public decimal? AddedWeight { get; set; }

    public List<string> Tags { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => ExerciseTagText.Validate(Tags);
}
