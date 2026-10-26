using System.Globalization;

/// <summary>
/// Spanish date/number formatting WITHOUT ICU. The WASM build runs with
/// InvariantGlobalization=true (#37 cold-start payload trim), so
/// <c>GetCultureInfo("es-ES")</c> throws CultureNotFoundException at runtime —
/// invisible to server-side unit tests, which run with full ICU.
///
/// Reproduces the exact output the previous culture-based formatting produced
/// ("26 oct 2026", "lunes, 26 de octubre", "12,5") using static tables, so
/// user-visible strings do not change. Add no ICU-dependent API here: any
/// CultureInfo other than InvariantCulture crashes on WASM.
/// </summary>
public static class EsFormat
{
    private static readonly string[] MonthsAbbr =
        ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sept", "oct", "nov", "dic"];

    private static readonly string[] MonthsFull =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio",
         "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    private static readonly string[] Weekdays =
        ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    // "d MMM" — "26 oct"
    public static string DayMonth(DateOnly date) => $"{date.Day} {MonthsAbbr[date.Month - 1]}";
    public static string DayMonth(DateTime date) => DayMonth(DateOnly.FromDateTime(date));

    // "d MMM yyyy" — "26 oct 2026"
    public static string DayMonthYear(DateOnly date) => $"{date.Day} {MonthsAbbr[date.Month - 1]} {date.Year}";
    public static string DayMonthYear(DateTime date) => DayMonthYear(DateOnly.FromDateTime(date));

    // "dddd, d MMM" — "lunes, 26 oct"
    public static string WeekdayDayMonth(DateOnly date) => $"{Weekday(date.DayOfWeek)}, {DayMonth(date)}";
    public static string WeekdayDayMonth(DateTime date) => WeekdayDayMonth(DateOnly.FromDateTime(date));

    // "dddd, d 'de' MMMM" — "lunes, 26 de octubre"
    public static string WeekdayDayMonthFull(DateOnly date) =>
        $"{Weekday(date.DayOfWeek)}, {date.Day} de {MonthsFull[date.Month - 1]}";

    // es-ES decimal separator (comma) via invariant formatting.
    public static string Decimal(decimal value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',');

    private static string Weekday(DayOfWeek day) =>
        Weekdays[day == DayOfWeek.Sunday ? 6 : (int)day - 1];
}
