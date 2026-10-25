using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Date contract (t_249c6eec): an activity's day is a local day and it
/// travels as that day's midnight UTC. Register, edit prefill, history undo
/// and history grouping must all round-trip through WellbeingActivityDates
/// without timezone drift, and the prefilled "hoy" must never be a day the
/// API can consider future (UTC+ early-morning window).
/// </summary>
public class WellbeingActivityDateContractTests
{
    private static WellbeingActivityResource ResourceWithOccurredOnUtc(string occurredOnUtc)
        => JsonSerializer.Deserialize<WellbeingActivityResource>(
            $$"""
            {
              "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "type": "other",
              "dimension": "health",
              "occurredOnUtc": "{{occurredOnUtc}}",
              "durationMinutes": 30,
              "frequencyPerWeek": 2,
              "intensity": null,
              "temperatureCelsius": null,
              "cycles": null,
              "notes": null,
              "metadata": null,
              "createdAtUtc": "2026-10-25T00:00:00Z"
            }
            """,
            // Web defaults, like GetFromJsonAsync uses in the app client.
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    // ===== "hoy" prefill (finding B) =====

    [Fact]
    public void TodayForRegistration_Clamps_Local_Today_To_Utc_Today_In_The_UtcPlus_Morning_Window()
    {
        // 01:53 CEST on 2026-10-25: local today is 25 but the API's cutoff
        // (today's UTC date) is still 24 — 2026-10-25T00:00:00Z would 400.
        WellbeingActivityDates.TodayForRegistration(
                new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 24))
            .Should().Be(new DateOnly(2026, 10, 24));
    }

    [Fact]
    public void TodayForRegistration_Keeps_Local_Today_When_It_Is_Not_Future()
    {
        WellbeingActivityDates.TodayForRegistration(
                new DateOnly(2026, 10, 25), new DateOnly(2026, 10, 25))
            .Should().Be(new DateOnly(2026, 10, 25));

        // UTC- device: local today is a day behind the UTC today — keep local.
        WellbeingActivityDates.TodayForRegistration(
                new DateOnly(2026, 10, 23), new DateOnly(2026, 10, 24))
            .Should().Be(new DateOnly(2026, 10, 23));
    }

    // ===== Wire form and day extraction (finding A) =====

    [Fact]
    public void ToWire_Emits_Midnight_Utc_Of_The_Day()
        => WellbeingActivityDates.ToWire(new DateOnly(2026, 10, 24))
            .Should().Be("2026-10-24T00:00:00Z");

    [Theory]
    [InlineData("2026-10-24T00:00:00Z")] // current API serialization
    [InlineData("2026-10-24T00:00:00")]  // legacy serialization without 'Z'
    public void ToLocalDay_Reads_The_Utc_Date_Regardless_Of_Offset_Format(string occurredOnUtc)
        => WellbeingActivityDates.ToLocalDay(ResourceWithOccurredOnUtc(occurredOnUtc).OccurredOnUtc)
            .Should().Be(new DateOnly(2026, 10, 24));

    // ===== Undo re-POST (finding A drift) =====

    [Fact]
    public void Undo_Mapper_Restores_The_Same_Day_As_Midnight_Utc()
    {
        var request = WellbeingActivityMapper.ToWriteRequest(ResourceWithOccurredOnUtc("2026-10-24T00:00:00Z"));
        request.OccurredOnUtc.Should().Be("2026-10-24T00:00:00Z");
    }

    [Fact]
    public void Undo_Mapper_Normalizes_Any_Intraday_Instant_To_The_Days_Midnight_Utc()
    {
        // A previously drifted record (QA saw 2026-10-22T20:00:00) must never
        // carry its time-of-day into the restored record.
        var request = WellbeingActivityMapper.ToWriteRequest(ResourceWithOccurredOnUtc("2026-10-22T20:00:00Z"));
        request.OccurredOnUtc.Should().Be("2026-10-22T00:00:00Z");
    }
}
