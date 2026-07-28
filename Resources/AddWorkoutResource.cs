public class AddWorkoutResource
{
    public DateTime? Date { get; set; }
    public string Type { get; set; } = "Training";
    public string? UserId { get; set; }
}
