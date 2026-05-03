public sealed class MuscleLoadRegionResource
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public double Load { get; set; }
    public double RelativeLoad { get; set; }
}

public sealed class WeeklyBodyLoadSnapshot
{
    public DateTime WeekStart { get; set; }
    public double TotalLoad { get; set; }
    public int WorkoutCount { get; set; }
    public int ExerciseCount { get; set; }
    public List<MuscleLoadRegionResource> Regions { get; set; } = new();
    public List<WeeklyWorkoutSessionResource> Sessions { get; set; } = new();
}

public sealed class WeeklyWorkoutSessionResource
{
    public string Key { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public double TotalLoad { get; set; }
    public int ExerciseCount { get; set; }
    public List<string> FocusGroups { get; set; } = new();
    public List<WeeklyWorkoutExerciseResource> Exercises { get; set; } = new();
}

public sealed class WeeklyWorkoutExerciseResource
{
    public string Name { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
    public string SessionLabel { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public List<string> PrimaryMuscles { get; set; } = new();
    public List<string> SecondaryMuscles { get; set; } = new();
    public double Load { get; set; }
    public double? MaxRepetition { get; set; }
    public List<string> Tags { get; set; } = new();
}

public sealed class AdaptationRadarAxis
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class AdaptationRadarSeries
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#0f5f67";
    public string? Caption { get; set; }
    public Dictionary<string, double> Values { get; set; } = new();
}
