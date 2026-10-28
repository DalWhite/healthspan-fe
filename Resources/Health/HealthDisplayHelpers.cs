public static class HealthDisplayHelpers
{
    public static string GetProcessingStatusLabel(BloodTestProcessingStatus status) => status switch
    {
        BloodTestProcessingStatus.PendingReview => "Pendiente de revisión",
        BloodTestProcessingStatus.Processed => "Procesada",
        BloodTestProcessingStatus.ExtractionFailed => "Extracción fallida",
        BloodTestProcessingStatus.Processing => "En proceso…",
        _ => "Desconocido"
    };

    public static string GetMarkerStatusLabel(MarkerStatus status, double? value = null, double? min = null, double? max = null)
    {
        if (IsOutOfRange(value, min, max))
        {
            if (min.HasValue && value.HasValue && value.Value < min.Value)
            {
                return "Bajo";
            }

            if (max.HasValue && value.HasValue && value.Value > max.Value)
            {
                return "Alto";
            }
        }

        return status switch
        {
            MarkerStatus.InRange => "En rango",
            MarkerStatus.Low => "Bajo",
            MarkerStatus.High => "Alto",
            MarkerStatus.RequiresReview => "Requiere revisión",
            _ => "Revisión"
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
            return "Lípidos";
        }

        if (raw.Contains("vitamin") || raw.Contains("ferritin") || raw.Contains("glucose") || raw.Contains("a1c") || raw.Contains("insulin"))
        {
            return "Metabólico";
        }

        if (raw.Contains("testosterone") || raw.Contains("estradiol") || raw.Contains("tsh") || raw.Contains("t3") || raw.Contains("t4"))
        {
            return "Hormonas";
        }

        if (raw.Contains("crp") || raw.Contains("esr") || raw.Contains("inflamm"))
        {
            return "Inflamación";
        }

        if (raw.Contains("hemoglobin") || raw.Contains("hemat") || raw.Contains("platelet") || raw.Contains("wbc") || raw.Contains("rbc"))
        {
            return "Hemograma";
        }

        return "Otros";
    }

    public static string ResolveAdaptationLabel(GalpinAdaptationType adaptation) => adaptation switch
    {
        GalpinAdaptationType.Skill => "Habilidad",
        GalpinAdaptationType.Strength => "Fuerza",
        GalpinAdaptationType.Power => "Potencia",
        GalpinAdaptationType.Speed => "Velocidad",
        GalpinAdaptationType.Hypertrophy => "Hipertrofia",
        GalpinAdaptationType.MuscularEndurance => "Resistencia muscular",
        GalpinAdaptationType.AnaerobicCapacity => "Capacidad anaeróbica",
        GalpinAdaptationType.MaximalAerobicPower => "Potencia aeróbica máxima",
        GalpinAdaptationType.LongDurationEndurance => "Resistencia de larga duración",
        _ => adaptation.ToString()
    };
}
