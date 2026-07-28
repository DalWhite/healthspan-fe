public enum HealthBiologicalSex
{
    Unknown,
    Female,
    Male
}

public sealed class HealthProfileResource
{
    public string? Name { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public HealthBiologicalSex BiologicalSex { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
}

public sealed class UpdateHealthProfileResource
{
    public string? Name { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public HealthBiologicalSex BiologicalSex { get; set; }
    public int? HeightCm { get; set; }
    public int? WeightKg { get; set; }
}
