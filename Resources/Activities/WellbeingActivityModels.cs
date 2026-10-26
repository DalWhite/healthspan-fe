#nullable enable
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// Models, type catalog and payload mapper for /api/activities.
// Contract fixed in t_0e23bbe6 (api-contract-activities-t_0e23bbe6.md):
// dimension travels as the canonical string (strength|health|mobility),
// occurredOnUtc is explicit UTC, per-type dynamic params travel inside
// `metadata`, and unknown root fields are rejected with 400.

/// <summary>
/// Single source of truth for the activity-date contract (t_249c6eec):
/// an activity's day is a LOCAL day and it travels as that day's midnight
/// UTC — "{yyyy-MM-dd}T00:00:00Z". Every path (register, edit prefill,
/// history undo, history day-grouping) goes through these helpers so the
/// same DateOnly round-trips unchanged; no path may apply local-time
/// conversions to the stored instant (that was the -1 day drift).
/// </summary>
public static class WellbeingActivityDates
{
    /// <summary>Wire form of a day: midnight UTC of that calendar day.</summary>
    public static string ToWire(DateOnly day)
        => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00Z";

    /// <summary>
    /// Day of a stored occurredOnUtc. By contract the instant is midnight UTC
    /// of the activity's day, so the UTC date IS the day — reading it must
    /// never go through the device's local timezone.
    /// </summary>
    public static DateOnly ToLocalDay(DateTimeOffset occurredOnUtc)
        => DateOnly.FromDateTime(occurredOnUtc.UtcDateTime);

    /// <summary>
    /// Default "hoy" for the register form: the API rejects any day after
    /// today's UTC date, and midnight UTC of the LOCAL today is in the future
    /// during the first hours of a UTC+ day. Clamp to the UTC today then, so
    /// the prefilled date is always acceptable (t_249c6eec finding B).
    /// </summary>
    public static DateOnly TodayForRegistration()
        => TodayForRegistration(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.UtcNow));

    public static DateOnly TodayForRegistration(DateOnly localToday, DateOnly utcToday)
        => localToday > utcToday ? utcToday : localToday;
}

/// <summary>
/// Reads occurredOnUtc as an absolute instant: strings with 'Z' parse exactly,
/// strings without offset (legacy API responses) are assumed UTC instead of
/// being treated as device-local — the conversion that produced the -1 day
/// drift on edit (t_249c6eec finding A).
/// </summary>
public sealed class UtcInstantConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String
            || !DateTimeOffset.TryParse(
                reader.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var value))
        {
            throw new JsonException("Expected an ISO-8601 UTC instant for occurredOnUtc.");
        }

        return value;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
        => writer.WriteStringValue(
            value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
}

public static class ActivityRoutes
{
    public const string Activities = "/api/activities";
}

/// <summary>
/// UI dimension keys (Spanish labels, UX spec §3) mapped to the canonical
/// API keys. The backend never accepts the label — the FE must translate.
/// </summary>
public static class WellbeingDimensions
{
    public static readonly IReadOnlyList<(string Key, string Label)> Options = new[]
    {
        ("fuerza", "Fuerza"),
        ("salud", "Salud"),
        ("movilidad", "Movilidad"),
    };

    private static readonly Dictionary<string, string> UiToCanonical = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fuerza"] = "strength",
        ["salud"] = "health",
        ["movilidad"] = "mobility",
    };

    public static string ToCanonical(string uiKey)
        => UiToCanonical.TryGetValue(uiKey, out var canonical)
            ? canonical
            : throw new ArgumentException($"Unknown dimension '{uiKey}'.", nameof(uiKey));

    public static string ToUiKey(string? canonical)
        => (canonical ?? string.Empty).ToLowerInvariant() switch
        {
            "strength" => "fuerza",
            "health" => "salud",
            "mobility" => "movilidad",
            _ => "salud",
        };
}

public enum ActivityFieldKind
{
    Number,
    Text,
    Select,
    Chips,
}

public sealed record ActivityFieldSpec(
    string Key,
    string Label,
    ActivityFieldKind Kind,
    bool Required = false,
    int Min = 0,
    int Max = 0,
    int MaxLength = 0,
    string[]? Options = null);

public sealed record ActivityTypeSpec(
    string Key,
    string Label,
    string Dimension,
    IReadOnlyList<ActivityFieldSpec> Fields);

/// <summary>
/// Static type catalog mirroring the register prototype's TYPES object
/// (register-activity-t_c35cb312.html §4 of the UX spec). Any type the
/// backend reports that is not in this catalog renders through the "other"
/// branch (chip "Otra actividad" with a name field).
/// </summary>
public static class WellbeingActivityTypes
{
    public const string OtherKey = "other";

    public static readonly IReadOnlyDictionary<string, ActivityTypeSpec> All =
        new Dictionary<string, ActivityTypeSpec>
        {
            ["sauna"] = new("sauna", "Sauna", "salud", new[]
            {
                new ActivityFieldSpec("durationMinutes", "Duración (min)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 1440),
                new ActivityFieldSpec("temperatureC", "Temperatura (°C)", ActivityFieldKind.Number, Min: -100, Max: 200),
                new ActivityFieldSpec("cycles", "Ciclos (sesiones)", ActivityFieldKind.Number, Min: 1, Max: 50),
                new ActivityFieldSpec("intensity", "Intensidad", ActivityFieldKind.Chips, Min: 1, Max: 5),
            }),
            ["cold_exposure"] = new("cold_exposure", "Exposición al frío", "salud", new[]
            {
                new ActivityFieldSpec("durationMinutes", "Duración (min)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 1440),
                new ActivityFieldSpec("temperatureC", "Temperatura (°C)", ActivityFieldKind.Number, Min: -100, Max: 200),
                new ActivityFieldSpec("intensity", "Intensidad", ActivityFieldKind.Chips, Min: 1, Max: 5),
            }),
            ["wim_hof"] = new("wim_hof", "Respiración Wim Hof", "salud", new[]
            {
                new ActivityFieldSpec("cycles", "Ciclos (rondas)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 50),
                new ActivityFieldSpec("durationMinutes", "Duración total (min)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 1440),
                new ActivityFieldSpec("breathsPerRound", "Respiraciones por ronda", ActivityFieldKind.Number, Min: 1, Max: 100),
            }),
            ["meditation"] = new("meditation", "Meditación", "salud", new[]
            {
                new ActivityFieldSpec("durationMinutes", "Duración (min)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 1440),
                new ActivityFieldSpec("style", "Estilo", ActivityFieldKind.Select, Options: new[] { "Guiada", "Silenciosa", "Caminando" }),
            }),
            [OtherKey] = new(OtherKey, "Otra actividad", "salud", new[]
            {
                new ActivityFieldSpec("name", "Nombre de la actividad", ActivityFieldKind.Text, Required: true, MaxLength: 80),
                new ActivityFieldSpec("durationMinutes", "Duración (min)", ActivityFieldKind.Number, Required: true, Min: 1, Max: 1440),
            }),
        };

    public static ActivityTypeSpec Resolve(string? typeKey)
        => typeKey is not null && All.TryGetValue(typeKey, out var spec) ? spec : All[OtherKey];

    /// <summary>
    /// Contract §2: durationMinutes, cycles, intensity and temperatureC
    /// (renamed to temperatureCelsius) are first-class root fields; every
    /// other per-type parameter (name, style, breathsPerRound, extra rows)
    /// must travel inside `metadata` or the API rejects the payload with 400.
    /// </summary>
    public static bool IsMetadataField(string key)
        => key is not ("durationMinutes" or "cycles" or "intensity" or "temperatureC");
}

/// <summary>Inbound payload for POST/PUT /api/activities (contract §2).</summary>
public sealed class WellbeingActivityWriteRequest
{
    public string Type { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string OccurredOnUtc { get; set; } = string.Empty;
    public int? DurationMinutes { get; set; }
    public int? FrequencyPerWeek { get; set; }
    public int? Intensity { get; set; }
    public int? TemperatureCelsius { get; set; }
    public int? Cycles { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>Outbound resource mirroring WellnessActivityResource.</summary>
public sealed class WellbeingActivityResource
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    [JsonConverter(typeof(UtcInstantConverter))]
    public DateTimeOffset OccurredOnUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? FrequencyPerWeek { get; set; }
    public decimal? Intensity { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public int? Cycles { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, JsonElement>? Metadata { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Paged envelope for GET /api/activities (contract §3).</summary>
public sealed class PagedActivitiesResource
{
    public List<WellbeingActivityResource> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// Server-side filter/pagination params for GET /api/activities (history
/// UX spec §3/§6): every filter travels as a query param — the FE never
/// filters or paginates in memory. Dimension is translated to the canonical
/// string before it leaves the FE.
/// </summary>
public sealed class ActivityQuery
{
    /// <summary>
    /// Window size for the history (t_66002472): one page is one view — the
    /// page renders at most 30 rows and fetches at most 30 items (~9KB), so
    /// DOM rows and per-view payload stay bounded no matter how deep the user
    /// pages. The API accepts pageSize 1..100.
    /// </summary>
    public const int DefaultPageSize = 30;

    public string? Type { get; init; }
    public string? DimensionUiKey { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public string ToQueryString()
    {
        var pairs = new List<string>();
        if (!string.IsNullOrWhiteSpace(Type))
        {
            pairs.Add($"type={Uri.EscapeDataString(Type)}");
        }
        if (!string.IsNullOrWhiteSpace(DimensionUiKey))
        {
            pairs.Add($"dimension={Uri.EscapeDataString(WellbeingDimensions.ToCanonical(DimensionUiKey))}");
        }
        if (From is DateOnly from)
        {
            pairs.Add($"from={from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }
        if (To is DateOnly to)
        {
            pairs.Add($"to={to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }
        pairs.Add($"page={Page}");
        pairs.Add($"pageSize={PageSize}");
        return string.Join("&", pairs);
    }
}

public sealed class ExtraFieldRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Presentation helpers for the history list/detail (history UX spec §4/§6):
/// row title fallbacks, main-param badges, dimension badge, day-group labels.
/// Pure functions so the page stays thin and the rules are unit-testable.
/// </summary>
public static class WellbeingActivityDisplay
{
    private const string WimHofKey = "wim_hof";

    /// <summary>
    /// Row title: type label from the catalog; for "Otra actividad" the
    /// metadata.name when present; unknown types render the raw type string
    /// (history UX spec §6 — never crash on a type the FE doesn't know).
    /// </summary>
    public static string RowTitle(WellbeingActivityResource resource)
    {
        if (resource.Type == WellbeingActivityTypes.OtherKey
            && resource.Metadata is { } metadata
            && metadata.TryGetValue("name", out var name)
            && name.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(name.GetString()))
        {
            return name.GetString()!;
        }

        return WellbeingActivityTypes.All.TryGetValue(resource.Type, out var spec)
            ? spec.Label
            : resource.Type;
    }

    /// <summary>Dimension badge UI label ("Fuerza"/"Salud"/"Movilidad") via the shared UI-key mapping.</summary>
    public static string DimensionBadgeLabel(WellbeingActivityResource resource)
    {
        var uiKey = WellbeingDimensions.ToUiKey(resource.Dimension);
        return WellbeingDimensions.Options.FirstOrDefault(o => o.Key == uiKey).Label;
    }

    public static string DimensionBadgeClass(WellbeingActivityResource resource)
        => $"hs-hist-badge-dim-{WellbeingDimensions.ToUiKey(resource.Dimension)}";

    /// <summary>
    /// Compact param badges, max 3 (spec §4): temperature, cycles ("rondas" for
    /// Wim Hof), intensity, style/series from metadata.
    /// </summary>
    public static List<string> MainParamBadges(WellbeingActivityResource resource)
    {
        var badges = new List<string>();

        if (resource.TemperatureCelsius is decimal temperature)
        {
            badges.Add($"{FormatNumber(temperature)} °C");
        }

        if (resource.Cycles is int cycles)
        {
            badges.Add(resource.Type == WimHofKey
                ? $"{cycles} rondas"
                : $"{cycles} ciclos");
        }

        if (resource.Intensity is decimal intensity)
        {
            badges.Add($"int. {FormatNumber(intensity)}");
        }

        if (resource.Metadata is { } metadata)
        {
            if (metadata.TryGetValue("style", out var style) && style.ValueKind == JsonValueKind.String)
            {
                badges.Add(style.GetString() ?? string.Empty);
            }
            else if (metadata.TryGetValue("series", out var series) && series.ValueKind == JsonValueKind.Number)
            {
                badges.Add($"{series.GetDecimal()} series");
            }
            else if (metadata.TryGetValue("series", out var textSeries) && textSeries.ValueKind == JsonValueKind.String)
            {
                badges.Add(textSeries.GetString() ?? string.Empty);
            }
        }

        return badges.Take(3).ToList();
    }

    /// <summary>
    /// Day-group heading: "Hoy" / "Ayer" / long es-ES date (spec §4). The API
    /// stores occurredOnUtc at midnight UTC of the user's local date, so the
    /// UTC date IS the activity's day (WellbeingActivityDates.ToLocalDay).
    /// </summary>
    public static string DayLabel(DateOnly day, DateOnly todayLocal)
    {
        if (day == todayLocal)
        {
            return "Hoy";
        }
        if (day == todayLocal.AddDays(-1))
        {
            return "Ayer";
        }

        return day.ToString("dddd, d 'de' MMMM", new CultureInfo("es-ES"));
    }

    public static string DayCountLabel(int count) => count == 1 ? "1 actividad" : $"{count} actividades";

    public static string DurationLabel(int? minutes) => minutes is int value ? $"{value} min" : string.Empty;

    private static string FormatNumber(decimal value)
        => value == decimal.Truncate(value)
            ? ((int)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Pure mapping layer from the register form state to the API payload.
/// Kept out of the page so the dynamic→metadata mapping is directly testable.
/// </summary>
public static class WellbeingActivityMapper
{
    public static WellbeingActivityWriteRequest ToWriteRequest(
        string typeKey,
        string dimensionUiKey,
        DateOnly occurredOn,
        IReadOnlyDictionary<string, string?> fieldValues,
        IEnumerable<ExtraFieldRow> extras,
        string? notes,
        string? frequencyPerWeek)
    {
        var request = new WellbeingActivityWriteRequest
        {
            Type = typeKey,
            Dimension = WellbeingDimensions.ToCanonical(dimensionUiKey),
            OccurredOnUtc = WellbeingActivityDates.ToWire(occurredOn),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        Dictionary<string, object?>? metadata = null;

        foreach (var (key, raw) in fieldValues)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            switch (key)
            {
                case "durationMinutes":
                    request.DurationMinutes = ParseInt(raw);
                    break;
                case "cycles":
                    request.Cycles = ParseInt(raw);
                    break;
                case "intensity":
                    request.Intensity = ParseInt(raw);
                    break;
                case "temperatureC":
                    request.TemperatureCelsius = ParseInt(raw);
                    break;
                default:
                    metadata ??= new Dictionary<string, object?>();
                    metadata[key] = ParseMetadataValue(raw);
                    break;
            }
        }

        foreach (var row in extras)
        {
            if (string.IsNullOrWhiteSpace(row.Key))
            {
                continue;
            }

            metadata ??= new Dictionary<string, object?>();
            metadata[row.Key.Trim()] = ParseMetadataValue(row.Value);
        }

        request.Metadata = metadata;

        if (int.TryParse((frequencyPerWeek ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var frequency))
        {
            request.FrequencyPerWeek = frequency;
        }

        return request;
    }

    /// <summary>
    /// Inbound resource → outbound write payload, used by the history "Deshacer"
    /// (re-POST of the deleted activity; history UX spec §6 — undo ≤6 s, new id
    /// is acceptable). Dimension arrives canonical from the API and travels back
    /// unchanged.
    /// </summary>
    public static WellbeingActivityWriteRequest ToWriteRequest(WellbeingActivityResource resource)
    {
        var request = new WellbeingActivityWriteRequest
        {
            Type = resource.Type,
            Dimension = resource.Dimension,
            // Normalize to the day's midnight UTC (WellbeingActivityDates):
            // undo must restore the exact day, never re-apply an offset —
            // that drift moved restored records a day back (t_249c6eec A).
            OccurredOnUtc = WellbeingActivityDates.ToWire(
                WellbeingActivityDates.ToLocalDay(resource.OccurredOnUtc)),
            DurationMinutes = resource.DurationMinutes,
            Intensity = resource.Intensity is decimal intensity ? (int)intensity : null,
            TemperatureCelsius = resource.TemperatureCelsius is decimal temperature ? (int)temperature : null,
            Cycles = resource.Cycles,
            Notes = resource.Notes,
        };

        if (resource.FrequencyPerWeek is decimal frequency)
        {
            request.FrequencyPerWeek = (int)frequency;
        }

        if (resource.Metadata is { Count: > 0 } metadata)
        {
            request.Metadata = new Dictionary<string, object?>();
            foreach (var (key, value) in metadata)
            {
                request.Metadata[key] = value.ValueKind switch
                {
                    JsonValueKind.Number => value.TryGetInt32(out var number) ? number : value.GetDecimal(),
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => value.ToString(),
                };
            }
        }

        return request;
    }

    private static int? ParseInt(string raw)
        => int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static object? ParseMetadataValue(string raw)
        => int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : raw;
}
