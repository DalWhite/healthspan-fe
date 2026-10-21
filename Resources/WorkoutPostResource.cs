public class WorkoutPostResource
{
    public string Type { get; set; } = "default";
    public string? UserId { get; set; }
    // Make Date nullable to match MudDatePicker's nullable Date type and avoid EventCallback type mismatch
    public DateTime? Date { get; set; } = DateTime.Now;

    // Universal tracker intent. Optional: a session can be logged before the
    // user names its goal, and refined later with PATCH /Workouts/{id}.
    public string? Goal { get; set; }
    public List<string> SecondaryGoals { get; set; } = [];
    public string? Modality { get; set; }
}

/// <summary>PATCH /Workouts/{id} — intent and the close-session fatigue score.</summary>
public class WorkoutPatchResource
{
    public string? Type { get; set; }
    public DateTime? Date { get; set; }
    public string? Goal { get; set; }
    public List<string>? SecondaryGoals { get; set; }
    public string? Modality { get; set; }
    public int? PerceivedFatigue { get; set; }
}