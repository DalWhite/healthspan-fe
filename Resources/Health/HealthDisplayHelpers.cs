public static class HealthDisplayHelpers
{
    public static string GetProcessingStatusLabel(BloodTestProcessingStatus status) => status switch
    {
        BloodTestProcessingStatus.PendingReview => "Pending review",
        BloodTestProcessingStatus.Processed => "Processed",
        BloodTestProcessingStatus.ExtractionFailed => "Extraction failed",
        BloodTestProcessingStatus.Processing => "Processing...",
        _ => "Unknown"
    };

    public static string GetMarkerStatusLabel(MarkerStatus status, double? value = null, double? min = null, double? max = null)
    {
        if (IsOutOfRange(value, min, max))
        {
            if (min.HasValue && value.HasValue && value.Value < min.Value)
            {
                return "Low";
            }

            if (max.HasValue && value.HasValue && value.Value > max.Value)
            {
                return "High";
            }
        }

        return status switch
        {
            MarkerStatus.InRange => "In range",
            MarkerStatus.Low => "Low",
            MarkerStatus.High => "High",
            MarkerStatus.RequiresReview => "Requires review",
            _ => "Review"
        };
    }

    public static bool IsOutOfRange(double? value, double? min, double? max)
    {
        if (!value.HasValue)
        {
            return false;
        }

        if (min.HasValue && value.Value < min.Value)
        {
            return true;
        }

        if (max.HasValue && value.Value > max.Value)
        {
            return true;
        }

        return false;
    }

    public static string ResolveMarkerCategory(string? metricKey, string? displayName)
    {
        var raw = $"{metricKey} {displayName}".ToLowerInvariant();

        if (raw.Contains("ldl") || raw.Contains("hdl") || raw.Contains("trig") || raw.Contains("chol") || raw.Contains("apob"))
        {
            return "Lipids";
        }

        if (raw.Contains("vitamin") || raw.Contains("ferritin") || raw.Contains("glucose") || raw.Contains("a1c") || raw.Contains("insulin"))
        {
            return "Metabolic";
        }

        if (raw.Contains("testosterone") || raw.Contains("estradiol") || raw.Contains("tsh") || raw.Contains("t3") || raw.Contains("t4"))
        {
            return "Hormones";
        }

        if (raw.Contains("crp") || raw.Contains("esr") || raw.Contains("inflamm"))
        {
            return "Inflammation";
        }

        if (raw.Contains("hemoglobin") || raw.Contains("hemat") || raw.Contains("platelet") || raw.Contains("wbc") || raw.Contains("rbc"))
        {
            return "CBC";
        }

        return "Other";
    }

    public static string ResolveAdaptationLabel(GalpinAdaptationType adaptation) => adaptation switch
    {
        GalpinAdaptationType.Skill => "Skill",
        GalpinAdaptationType.Strength => "Strength",
        GalpinAdaptationType.Power => "Power",
        GalpinAdaptationType.Speed => "Speed",
        GalpinAdaptationType.Hypertrophy => "Hypertrophy",
        GalpinAdaptationType.MuscularEndurance => "Muscular Endurance",
        GalpinAdaptationType.AnaerobicCapacity => "Anaerobic Capacity",
        GalpinAdaptationType.MaximalAerobicPower => "Maximal Aerobic Power",
        GalpinAdaptationType.LongDurationEndurance => "Long Duration Endurance",
        _ => adaptation.ToString()
    };
}
