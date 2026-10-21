public static class TrainingTrackerRoutes
{
    public const string AdaptationCoverage = "/Adaptations/coverage";
    public const string Readiness = "/Readiness";
}

/// <summary>Weekly coverage of the 9 Galpin adaptations (GET /adaptations/coverage).</summary>
public sealed class AdaptationCoverageResource
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Sessions { get; set; }
    public List<AdaptationCoverageItemResource> Items { get; set; } = [];
}

public sealed class AdaptationCoverageItemResource
{
    public int AdaptationId { get; set; }
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Volume { get; set; }
    public int Sessions { get; set; }
    public int TargetSessionsPerWeek { get; set; }
    /// <summary>Sessions / target. 1 = covered, 0 = never trained.</summary>
    public double CoverageRatio { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public int? DaysSinceLastSession { get; set; }
    /// <summary>up | flat | down | none, compared with the previous window.</summary>
    public string Trend { get; set; } = "none";
}

/// <summary>Derived readiness (GET /readiness). 10 = rested and ready, 0 = exhausted.</summary>
public sealed class ReadinessResource
{
    public DateTime Date { get; set; }
    public double Score { get; set; }
    public double FatigueLoad7d { get; set; }
    public int Sessions7d { get; set; }
    public double TotalVolume7d { get; set; }
    public List<ReadinessAdaptationResource> PerAdaptation { get; set; } = [];
    public List<ReadinessSuggestionResource> Suggestions { get; set; } = [];
}

public sealed class ReadinessAdaptationResource
{
    public int AdaptationId { get; set; }
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Volume7d { get; set; }
    public int Sessions7d { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public int? DaysSinceLastSession { get; set; }
    public string Trend { get; set; } = "none";
}

public sealed class ReadinessSuggestionResource
{
    public int AdaptationId { get; set; }
    public int Order { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Short, glanceable names for the nine adaptations. The API returns the full
/// catalog names; the radar axes and the readiness copy use these short forms
/// so the dashboard reads at a glance (no physiological jargon).
/// </summary>
public static class AdaptationNames
{
    private static readonly Dictionary<string, string> ShortNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Skill"] = "Habilidad",
        ["Speed"] = "Velocidad",
        ["Power"] = "Potencia",
        ["Strength"] = "Fuerza",
        ["Hypertrophy"] = "Hipertrofia",
        ["MuscularEndurance"] = "Resistencia muscular",
        ["AnaerobicCapacity"] = "Anaeróbica",
        ["MaximalAerobicPower"] = "Aeróbica máxima",
        ["LongDurationEndurance"] = "Larga duración"
    };

    public static string Short(string type, string? fallback = null)
        => ShortNames.TryGetValue(type ?? string.Empty, out var name)
            ? name
            : (!string.IsNullOrWhiteSpace(fallback) ? fallback! : "Adaptación");
}
