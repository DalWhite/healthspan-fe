public class Muscle
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Position { get; set; }    // legacy alias for "front" or "back"
    public string? Side { get; set; }
    public string? MuscleGroup { get; set; } // e.g., "Arms", "Legs", "Chest"
}
