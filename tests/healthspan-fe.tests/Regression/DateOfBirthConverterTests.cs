using System.Globalization;
using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The parsing and range rules behind the profile date-of-birth field, tested
/// directly so the table is pinned instead of inferred from the rendered page.
///
/// The bug this anchors: a lenient parse completes whatever the typed text
/// left out from today's date, so "10/16" became today and was saved with
/// "Profile saved." while producing an age-0 profile.
/// </summary>
public sealed class DateOfBirthConverterTests : IDisposable
{
    private readonly CultureInfo _originalCulture =
        CultureInfo.CurrentCulture;

    public void Dispose() => CultureInfo.CurrentCulture = _originalCulture;

    [Theory]
    // Complete dates a user can plausibly type.
    [InlineData("1990-05-15", 1990, 5, 15)]
    [InlineData("1990-5-15", 1990, 5, 15)]
    [InlineData("15/05/1990", 1990, 5, 15)]
    [InlineData("05/15/1990", 1990, 5, 15)]
    [InlineData("1990/05/15", 1990, 5, 15)]
    [InlineData("15-05-1990", 1990, 5, 15)]
    [InlineData("15 May 1990", 1990, 5, 15)]
    public void Accepted_Typed_Dates(string text, int year, int month, int day)
    {
        foreach (var culture in Cultures)
        {
            CultureInfo.CurrentCulture = culture;

            DateOfBirthConverter.TryParse(text, out var date)
                .Should().BeTrue($"'{text}' under {culture.Name}");
            date.Should().Be(new DateTime(year, month, day), $"'{text}' under {culture.Name}");
        }
    }

    [Theory]
    // Anything that is not a complete date: a lenient parse would fill the
    // missing part from today and store it silently.
    [InlineData("10/16")]
    [InlineData("16/10")]
    [InlineData("15/05")]
    [InlineData("May 15")]
    [InlineData("15 May")]
    [InlineData("1990")]
    [InlineData("1990-05")]
    [InlineData("15/05/90")]
    [InlineData("garbage")]
    [InlineData("31/13/1990")]
    [InlineData("1990-02-30")]
    public void Rejected_Typed_Text(string text)
    {
        foreach (var culture in Cultures)
        {
            CultureInfo.CurrentCulture = culture;

            DateOfBirthConverter.TryParse(text, out var date)
                .Should().BeFalse($"'{text}' under {culture.Name}");
            date.Should().BeNull();
        }
    }

    [Fact]
    public void Empty_Means_No_Date_Of_Birth_And_Is_Valid()
    {
        DateOfBirthConverter.TryParse(null, out var date)
            .Should().BeTrue();
        date.Should().BeNull();

        DateOfBirthConverter.TryParse("   ", out date)
            .Should().BeTrue();
        date.Should().BeNull();

        DateOfBirthConverter.Validate(null).Should().BeNull();
    }

    [Theory]
    [InlineData(1899, 12, 31)]
    [InlineData(1800, 1, 1)]
    public void A_Date_Before_The_Lower_Bound_Is_Refused_On_The_Field(
        int year, int month, int day)
        => DateOfBirthConverter
            .Validate(new DateTime(year, month, day))
            .Should().Be("Date of birth must be 1900-01-01 or later.");

    [Theory]
    [InlineData(2099, 1, 1)]
    public void A_Date_After_The_Upper_Bound_Is_Refused_On_The_Field(
        int year, int month, int day)
        => DateOfBirthConverter
            .Validate(new DateTime(year, month, day))
            .Should().Be("Date of birth must be a date before today.");

    // QA repro (t_343b882e): today used to be accepted, producing age 0.
    [Fact]
    public void Today_Is_Refused_Yesterday_Is_The_Last_Accepted_Date()
    {
        DateOfBirthConverter.Validate(DateTime.Today)
            .Should().Be("Date of birth must be a date before today.");

        DateOfBirthConverter.Validate(DateOfBirthConverter.MaxDateOfBirth)
            .Should().BeNull();

        DateOfBirthConverter.Validate(DateOfBirthConverter.MinDateOfBirth)
            .Should().BeNull();
    }

    // The wording has to match PUT /healthprofile so a date that slips
    // through is explained on the field, not as a generic alert.
    [Fact]
    public void The_Messages_Match_What_The_Backend_Returns()
    {
        DateOfBirthConverter.Validate(DateTime.Today)
            .Should().Be("Date of birth must be a date before today.");

        DateOfBirthConverter.Validate(new DateTime(1899, 12, 31))
            .Should().Be("Date of birth must be 1900-01-01 or later.");

        DateOfBirthConverter.UnparseableMessage
            .Should().Be("Enter a real date, for example 15/05/1990");
    }

    private static CultureInfo[] Cultures =>
    [
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("es-ES")
    ];
}
