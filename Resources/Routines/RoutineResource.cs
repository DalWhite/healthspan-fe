public class RoutineResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Coach who assigned this routine (null for self-created routines).
    /// Read-only: the server derives it from the authenticated requester.
    /// </summary>
    public string? TrainerId { get; set; }
    public int Version { get; set; }
    public List<WorkoutTemplateResource> WorkoutTemplates { get; set; } = [];
}
