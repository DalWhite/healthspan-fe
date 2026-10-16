using System.Globalization;
using MudBlazor;

/// <summary>
/// Date-of-birth rules for the profile date picker: which text may be typed
/// and which date may be saved.
///
/// MudBlazor's default converter parses typed text with
/// <c>DateTime.ParseExact</c> against the culture's short date pattern and
/// returns null when that fails, so a typed date was silently discarded and
/// saved as null with no message (QA report: typing "1990-05-15" lost the
/// value).
///
/// This converter accepts the formats a user can plausibly type and refuses
/// anything else with a specific, non-blaming message, so the picker explains
/// what went wrong instead of losing the value. Only complete dates are
/// accepted: a lenient parse fills in whatever the text left out from today's
/// date, which used to turn "10/16" into today and store an age-0 profile.
///
/// <see cref="Validate"/> is the one rule for the value, shared by the picker,
/// the page's save guard and the wording PUT /healthprofile enforces.
/// </summary>
public sealed class DateOfBirthConverter : DefaultConverter<DateTime?>
{
    /// <summary>Same lower bound as PUT /healthprofile.</summary>
    public static readonly DateTime MinDateOfBirth = new(1900, 1, 1);

    /// <summary>
    /// Same upper bound as PUT /healthprofile: a date of birth has to be a
    /// real past date, so today is already too late.
    /// </summary>
    public static DateTime MaxDateOfBirth => DateTime.Today.AddDays(-1);

    public const string UnparseableMessage =
        "Enter a real date, for example 15/05/1990";

    /// <summary>
    /// Clear the rejection state, e.g. when the profile is (re)loaded and the
    /// field starts from a known-good value.
    /// </summary>
    public void Reset()
    {
        GetError = false;
        GetErrorMessage = null;
    }

    /// <summary>
    /// null when the date may be saved, otherwise the message that explains
    /// why not. Empty is valid and means "no date of birth".
    /// </summary>
    public static string? Validate(DateTime? date)
    {
        if (date is null)
        {
            return null;
        }

        if (date.Value.Date > MaxDateOfBirth)
        {
            return "Date of birth must be a date before today.";
        }

        if (date.Value.Date < MinDateOfBirth)
        {
            return $"Date of birth must be {MinDateOfBirth:yyyy-MM-dd} or later.";
        }

        return null;
    }

    protected override DateTime? ConvertFromString(string value)
    {
        if (!TryParse(value, out var date))
        {
            UpdateGetError(UnparseableMessage);
            return null;
        }

        var message = Validate(date);
        if (message is not null)
        {
            UpdateGetError(message);
            return null;
        }

        Reset();
        return date;
    }

    /// <summary>
    /// The single parsing rule, shared by the converter and the tests. Empty
    /// text is valid and means "no date of birth"; anything else must name a
    /// day, a month and a four-digit year.
    /// </summary>
    public static bool TryParse(string? text, out DateTime? date)
    {
        date = null;

        if (string.IsNullOrWhiteSpace(text))
            return true;

        text = text.Trim();

        foreach (var culture in new[]
        {
            CultureInfo.CurrentCulture,
            CultureInfo.InvariantCulture
        })
        {
            if (DateTime.TryParseExact(
                    text,
                    AcceptedFormats(culture),
                    culture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                date = parsed.Date;
                return true;
            }
        }

        return false;
    }

    // Every format here has a day, a month and a year, so no part of the date
    // can be completed from the current date. The culture's own short pattern
    // is included so a user typing their locale's format keeps working.
    private static string[] AcceptedFormats(CultureInfo culture) =>
    [
        culture.DateTimeFormat.ShortDatePattern,
        "yyyy-M-d",
        "yyyy/M/d",
        "d/M/yyyy",
        "M/d/yyyy",
        "d-M-yyyy",
        "d MMMM yyyy",
        "MMMM d yyyy",
        "MMMM d, yyyy"
    ];
}
