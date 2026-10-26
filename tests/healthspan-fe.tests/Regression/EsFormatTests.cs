using System.Globalization;
using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// EsFormat must produce byte-identical output to the ICU es-ES culture it
/// replaced (server-side only — WASM cannot load ICU), so replacing the
/// culture calls changes no user-visible string.
/// </summary>
public class EsFormatTests
{
    [Theory]
    [MemberData(nameof(Dates))]
    public void Output_MatchesIcuEsEs(string isoDate)
    {
        var es = CultureInfo.GetCultureInfo("es-ES");
        var date = DateOnly.Parse(isoDate);
        var dateTime = date.ToDateTime(TimeOnly.MinValue);

        EsFormat.DayMonth(date).Should().Be(date.ToString("d MMM", es));
        EsFormat.DayMonth(dateTime).Should().Be(dateTime.ToString("d MMM", es));
        EsFormat.DayMonthYear(date).Should().Be(date.ToString("d MMM yyyy", es));
        EsFormat.DayMonthYear(dateTime).Should().Be(dateTime.ToString("d MMM yyyy", es));
        EsFormat.WeekdayDayMonth(date).Should().Be(dateTime.ToString("dddd, d MMM", es));
        EsFormat.WeekdayDayMonthFull(date).Should().Be(date.ToString("dddd, d 'de' MMMM", es));
    }

    [Fact]
    public void Decimal_MatchesIcuEsEs()
    {
        var es = CultureInfo.GetCultureInfo("es-ES");
        foreach (var value in new[] { 12.5m, 1234.5m, 19.99m, 9.99m, 0.1m, 89.99m, 123456.7m })
        {
            EsFormat.Decimal(value, "0.00").Should().Be(value.ToString("0.00", es));
            EsFormat.Decimal(value, "0.0").Should().Be(value.ToString("0.0", es));
            EsFormat.Decimal(value, "0").Should().Be(value.ToString("0", es));
        }
    }

    public static IEnumerable<object[]> Dates()
    {
        // Every month, plus edge days and every weekday over two weeks.
        var dates = new List<object[]>();
        for (var month = 1; month <= 12; month++)
        {
            foreach (var day in new[] { 1, 2, 14, 20, 26, 28, 29, 30, 31 })
            {
                if (day <= System.DateTime.DaysInMonth(2026, month))
                {
                    dates.Add(new object[] { $"2026-{month:00}-{day:00}" });
                }
            }
        }

        for (var offset = 0; offset < 14; offset++)
        {
            dates.Add(new object[] { new System.DateTime(2026, 10, 19).AddDays(offset).ToString("yyyy-MM-dd") });
        }

        return dates;
    }
}
