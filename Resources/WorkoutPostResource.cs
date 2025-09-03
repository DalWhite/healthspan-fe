public class WorkoutPostResource
{
    public string Type { get; set; } = "default";
    public string? UserId { get; set; }
    // Make Date nullable to match MudDatePicker's nullable Date type and avoid EventCallback type mismatch
    public DateTime? Date { get; set; } = DateTime.Now;
}