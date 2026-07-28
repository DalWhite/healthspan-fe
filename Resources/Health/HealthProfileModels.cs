public enum HealthBiologicalSex
{
    Unknown,
    Female,
    Male
}

public sealed class HealthProfileResource
{
    public DateOnly? DateOfBirth { get; set; }
    public HealthBiologicalSex BiologicalSex { get; set; }
}

public sealed class UpdateHealthProfileResource
{
    public DateOnly? DateOfBirth { get; set; }
    public HealthBiologicalSex BiologicalSex { get; set; }
}
