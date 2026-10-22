public class WorkoutTemplateResource
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int FrequencyPerWeek { get; set; } = 1;
    public List<ExercisePrescriptionResource> Exercises { get; set; } = [];
}
