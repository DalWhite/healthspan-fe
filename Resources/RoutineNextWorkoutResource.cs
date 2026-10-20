/// <summary>
/// The workout the trainee should do next, from their routines
/// (GET /routines/next). Mirrors the API contract.
/// </summary>
public sealed class RoutineNextWorkoutResource
{
    /// <summary>
    /// Day the workout becomes actionable: today while the weekly quota is
    /// not met, otherwise the Monday that resets it.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// True when the workout is pending right now, which is what makes
    /// "Start today's workout" available.
    /// </summary>
    public bool PendingNow { get; set; }

    public Guid RoutineId { get; set; }
    public string RoutineName { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int FrequencyPerWeek { get; set; }
    public List<RoutineExerciseResource> Exercises { get; set; } = [];
}

/// <summary>
/// One exercise slot of a routine workout. The coach's own wording for the
/// slot lives in Notes; the description from the exercise library is the
/// fallback shown when Notes is empty.
/// </summary>
public sealed class RoutineExerciseResource
{
    public int ExerciseTypeId { get; set; }
    public string? ExerciseTypeName { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public int? Sets { get; set; }
    public int? Reps { get; set; }
    public double? LoadKg { get; set; }
    public int? Series { get; set; }
    public int? RestSeconds { get; set; }
    public string? Notes { get; set; }

    public string LoadLabel
        => LoadKg is null or 0
            ? "Bodyweight"
            : $"{LoadKg.Value:0.##} kg";

    public string TargetLabel
    {
        get
        {
            var series = Series ?? Sets ?? 0;
            var reps = Reps ?? 0;
            if (series == 0 && reps == 0)
            {
                return "As prescribed";
            }

            if (reps == 0)
            {
                return $"{series} sets";
            }

            return series == 0
                ? $"{reps} reps"
                : $"{series} x {reps}";
        }
    }

    /// <summary>
    /// Coach's wording first, library description as the fallback.
    /// </summary>
    public string? Explanation
        => !string.IsNullOrWhiteSpace(Notes)
            ? Notes
            : !string.IsNullOrWhiteSpace(Description)
                ? Description
                : null;
}
