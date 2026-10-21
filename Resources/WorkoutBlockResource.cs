using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

/// <summary>
/// The shape of a block inside a session. Mirrors the API's WorkoutBlockType
/// exactly (Normal, Superset, Circuito, EMOM, AMRAP, Tabata, Ronda): the wire
/// carries the name as a string, so the FE must never rename a value.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkoutBlockType
{
    Normal,
    Superset,
    Circuito,
    EMOM,
    AMRAP,
    Tabata,
    Ronda
}

public static class WorkoutBlockTypeLabels
{
    public static string Label(this WorkoutBlockType type) => type switch
    {
        WorkoutBlockType.Normal => "Normal",
        WorkoutBlockType.Superset => "Superset",
        WorkoutBlockType.Circuito => "Circuit",
        WorkoutBlockType.EMOM => "EMOM",
        WorkoutBlockType.AMRAP => "AMRAP",
        WorkoutBlockType.Tabata => "Tabata",
        WorkoutBlockType.Ronda => "Round",
        _ => type.ToString()
    };

    /// <summary>The order a user meets them in: the plain case first, then the
    /// structures people actually build (NN/g: most-common options first).</summary>
    public static readonly WorkoutBlockType[] PickerOrder =
    [
        WorkoutBlockType.Normal,
        WorkoutBlockType.Superset,
        WorkoutBlockType.Circuito,
        WorkoutBlockType.EMOM,
        WorkoutBlockType.AMRAP,
        WorkoutBlockType.Tabata,
        WorkoutBlockType.Ronda
    ];

    public static WorkoutBlockType? Parse(string? value)
        => Enum.TryParse<WorkoutBlockType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
}

/// <summary>A block as returned by the API (server ids are real ids).</summary>
public sealed class WorkoutBlockResource
{
    public Guid Id { get; set; }
    public Guid? ClientEntityId { get; set; }
    public int Order { get; set; }
    public string? Type { get; set; }
    public string? Label { get; set; }
    public int? Rounds { get; set; }
    public List<WorkoutSessionExerciseResource> Exercises { get; set; } = [];
}

/// <summary>POST /Workouts/{workoutId}/blocks</summary>
public sealed class AddWorkoutBlockResource
{
    /// <summary>Offline client id. The server dedupes on it, so a retry of the
    /// same block never creates a second one.</summary>
    public Guid? ClientEntityId { get; set; }
    public int Order { get; set; } = 1;
    public string Type { get; set; } = nameof(WorkoutBlockType.Normal);
    [StringLength(100, ErrorMessage = "Block name must be 100 characters or less")]
    public string? Label { get; set; }
    [Range(1, 100, ErrorMessage = "Rounds must be between 1 and 100")]
    public int? Rounds { get; set; }
}
