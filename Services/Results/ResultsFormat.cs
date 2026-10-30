using System.Globalization;

namespace healthspan_fe_wasm_pwa.Services.Results;

/// <summary>
/// Presentation-only formatters for Results KPIs. They NEVER recompute or
/// re-round a KPI value (spec results-kpis-ux-t_33c412cf.md §6): the sign and
/// magnitude arrive resolved from the API. Allowed transforms only: literal
/// sign per E8, es-ES decimal comma (EsFormat pattern — WASM runs with
/// InvariantGlobalization, so no explicit Spanish CultureInfo either), NBSP between number
/// and unit, and the display-only ×100 of the 0..1 consistency score.
/// </summary>
public static class ResultsFormat
{
    public const string Nbsp = "\u00A0";
    private const string Minus = "\u2212"; // typographic minus, not hyphen

    /// <summary>0..1 score → "75" / "61,5" (no unit, no NBSP — caller adds unit).</summary>
    public static string Percent(double score0to1) =>
        ((decimal)score0to1 * 100m).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Weight delta kg: always 1 decimal (E8) → "−0,6" / "± 0,0".</summary>
    public static string DeltaKg(double delta) =>
        delta == 0 ? "± 0,0" : Signed(delta, OneDecimal(delta));

    /// <summary>Signed strength index → "+4,2" (always 1 decimal).</summary>
    public static string SignedIndex(double value) => Signed(value, OneDecimal(value));

    /// <summary>
    /// Per-type delta (E9): unit is never mixed. "%" keeps 1 decimal;
    /// kg/reps print integers without decimals and fractions with one
    /// (prototype: "+2,5 kg", "± 0 kg", "+6,0 %").
    /// </summary>
    public static string TypeDelta(double delta, string unit)
    {
        if (delta == 0) return unit == "%" ? "± 0,0" : "± 0";
        var text = unit == "%" || delta != Math.Truncate(delta) ? OneDecimal(delta) : ((long)delta).ToString(CultureInfo.InvariantCulture);
        return Signed(delta, text);
    }

    /// <summary>1RM-style value: integers clean, fractions one decimal → "81,4".</summary>
    public static string Num(double value) =>
        value != Math.Truncate(value) ? OneDecimal(value) : ((long)value).ToString(CultureInfo.InvariantCulture);

    /// <summary>Weight kg hero/points: always 1 decimal per E8 → "81,4".</summary>
    public static string Kg(double value) => OneDecimal(value);

    private static string Signed(double value, string magnitude) =>
        (value < 0 ? Minus : "+") + magnitude;

    private static string OneDecimal(double value) =>
        Math.Abs(value).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
}
