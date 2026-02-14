public record ExerciseTag
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "equipment", "mechanic", "level", "force", "type"
    public string? Icon { get; set; }
}
