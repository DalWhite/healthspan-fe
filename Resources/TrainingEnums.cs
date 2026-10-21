using System.Text.Json.Serialization;

/// <summary>
/// Why the session exists. Mirrors the API's GalpinAdaptationType exactly —
/// the wire format is the enum name as a string, so the FE must never invent
/// values or rename them, or a saved goal silently becomes "not set".
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrainingGoal
{
    Skill,
    Speed,
    Power,
    Strength,
    Hypertrophy,
    MuscularEndurance,
    AnaerobicCapacity,
    MaximalAerobicPower,
    LongDurationEndurance
}

/// <summary>
/// What kind of training the session was. Mirrors the API's WorkoutModality
/// (the names are Spanish on the API; the UI shows an English label).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrainingModality
{
    Fuerza,
    Funcional,
    Pliometria,
    Movilidad,
    JiuJitsu,
    Tenis,
    Kitesurf,
    Cardio,
    Caminata,
    Otro
}

/// <summary>
/// Where the load on a logged exercise comes from. Mirrors ExerciseLoadType.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoadType
{
    Bodyweight,
    External,
    Band,
    Kettlebell,
    Machine,
    Poles,
    Other
}

/// <summary>
/// Labels and ordering for the intent chips. The UI shows plain words; the
/// wire keeps the API names, so a label can change without touching the API.
/// </summary>
public static class TrainingIntent
{
    /// <summary>Goals in the order a first-time user sees them: the reasons
    /// people actually train come first (NN/g: most-common options first).</summary>
    public static readonly TrainingGoal[] GoalOrder =
    [
        TrainingGoal.Hypertrophy,
        TrainingGoal.Strength,
        TrainingGoal.MuscularEndurance,
        TrainingGoal.Power,
        TrainingGoal.MaximalAerobicPower,
        TrainingGoal.LongDurationEndurance,
        TrainingGoal.AnaerobicCapacity,
        TrainingGoal.Speed,
        TrainingGoal.Skill
    ];

    public static readonly TrainingModality[] ModalityOrder =
    [
        TrainingModality.Fuerza,
        TrainingModality.Funcional,
        TrainingModality.Cardio,
        TrainingModality.Movilidad,
        TrainingModality.Pliometria,
        TrainingModality.JiuJitsu,
        TrainingModality.Tenis,
        TrainingModality.Kitesurf,
        TrainingModality.Caminata,
        TrainingModality.Otro
    ];

    public static readonly LoadType[] LoadOrder =
    [
        LoadType.External,
        LoadType.Bodyweight,
        LoadType.Kettlebell,
        LoadType.Band,
        LoadType.Machine,
        LoadType.Poles,
        LoadType.Other
    ];

    public static string Label(this TrainingGoal goal) => goal switch
    {
        TrainingGoal.Skill => "Skill",
        TrainingGoal.Speed => "Speed",
        TrainingGoal.Power => "Power",
        TrainingGoal.Strength => "Strength",
        TrainingGoal.Hypertrophy => "Build muscle",
        TrainingGoal.MuscularEndurance => "Muscular endurance",
        TrainingGoal.AnaerobicCapacity => "Anaerobic capacity",
        TrainingGoal.MaximalAerobicPower => "Aerobic power",
        TrainingGoal.LongDurationEndurance => "Endurance",
        _ => goal.ToString()
    };

    public static string Label(this TrainingModality modality) => modality switch
    {
        TrainingModality.Fuerza => "Strength",
        TrainingModality.Funcional => "Functional",
        TrainingModality.Pliometria => "Plyometrics",
        TrainingModality.Movilidad => "Mobility",
        TrainingModality.JiuJitsu => "Jiu-jitsu",
        TrainingModality.Tenis => "Tennis",
        TrainingModality.Kitesurf => "Kitesurf",
        TrainingModality.Cardio => "Cardio",
        TrainingModality.Caminata => "Walking",
        TrainingModality.Otro => "Other",
        _ => modality.ToString()
    };

    public static string Label(this LoadType loadType) => loadType switch
    {
        LoadType.Bodyweight => "Bodyweight",
        LoadType.External => "External load",
        LoadType.Band => "Band",
        LoadType.Kettlebell => "Kettlebell",
        LoadType.Machine => "Machine",
        LoadType.Poles => "Poles",
        LoadType.Other => "Other load",
        _ => loadType.ToString()
    };

    /// <summary>Parse a wire value defensively: an unknown or missing value
    /// never crashes the page, it simply means "not chosen yet".</summary>
    public static TrainingGoal? ParseGoal(string? value)
        => Enum.TryParse<TrainingGoal>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;

    public static TrainingModality? ParseModality(string? value)
        => Enum.TryParse<TrainingModality>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;

    public static LoadType? ParseLoadType(string? value)
        => Enum.TryParse<LoadType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;

    public static List<string> Names(IEnumerable<TrainingGoal> goals)
        => goals.Select(g => g.ToString()).ToList();
}

/// <summary>
/// Perceived fatigue asked once, when the session is closed (1..5). Kept as a
/// plain int on the wire, exactly like the API's PerceivedFatigue.
/// </summary>
public static class PerceivedFatigue
{
    public const int Min = 1;
    public const int Max = 5;

    public static string Label(int value) => value switch
    {
        1 => "Very easy",
        2 => "Easy",
        3 => "Moderate",
        4 => "Hard",
        5 => "Max effort",
        _ => "Not set"
    };

    public static bool IsValid(int? value) => value is >= Min and <= Max;
}
