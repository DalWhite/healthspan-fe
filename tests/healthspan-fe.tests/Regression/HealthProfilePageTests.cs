using System.Net;
using System.Text;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;
using HealthProfilePage =
    healthspan_fe_wasm_pwa.Pages.Health.HealthProfile;

namespace healthspan_fe.tests.Regression;

public sealed class HealthProfilePageTests : BlazorTestBase
{
    // The profile page loads over HTTP before the form appears; on loaded
    // machines the render can take longer than bUnit's 5s default ceiling
    // (same ceiling already used by WorkoutsPageTests).
    private static readonly TimeSpan WaitCeiling = TimeSpan.FromSeconds(20);

    private const string EmptyProfile = """
        {
          "name": "David",
          "dateOfBirth": null,
          "biologicalSex": 0,
          "heightCm": null,
          "weightKg": null
        }
        """;

    [Fact]
    public void Renders_Saved_Health_Profile()
    {
        RespondWithProfile("1989-06-15");

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Personal profile");
            cut.Markup.Should().Contain("David");
            cut.Markup.Should().Contain("Current weight");
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Save"));
        }, WaitCeiling);
    }

    [Fact]
    public void Renders_Load_Error_Without_Hiding_Form()
    {
        MockHttp.When("/HealthProfile")
            .Throw(new HttpRequestException("offline"));

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain(
                "Profile could not be loaded");
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Save"));
        }, WaitCeiling);
    }

    // --- Date of birth: the value the picker holds (t_343b882e) -----------

    [Fact]
    public void Blocks_Save_When_Date_Of_Birth_Is_Today()
    {
        // QA repro: today's date used to be stored, producing age 0 and
        // corrupted blood-test age ranges.
        RespondWithProfile("1989-06-15");
        var saves = MockSaveEndpoint();

        var cut = RenderComponent<HealthProfilePage>();
        PickDate(cut, DateTime.Today);

        cut.WaitForAssertion(() => cut.Markup
            .Should().Contain("Date of birth must be a date before today."),
            WaitCeiling);

        SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        saves.Should().BeEmpty();
    }

    [Fact]
    public void Blocks_Save_When_Date_Of_Birth_Is_In_The_Future()
    {
        RespondWithProfile("1989-06-15");
        var saves = MockSaveEndpoint();

        var cut = RenderComponent<HealthProfilePage>();
        PickDate(cut, DateTime.Today.AddDays(30));

        cut.WaitForAssertion(() => cut.Markup
            .Should().Contain("Date of birth must be a date before today."),
            WaitCeiling);

        SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        saves.Should().BeEmpty();
    }

    [Fact]
    public void Blocks_Save_When_Date_Of_Birth_Is_Before_1900()
    {
        RespondWithProfile("1989-06-15");
        var saves = MockSaveEndpoint();

        var cut = RenderComponent<HealthProfilePage>();
        PickDate(cut, new DateTime(1899, 12, 31));

        cut.WaitForAssertion(() => cut.Markup
            .Should().Contain("Date of birth must be 1900-01-01 or later."),
            WaitCeiling);

        SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        saves.Should().BeEmpty();
    }

    [Fact]
    public void Surfaces_An_Impossible_Date_Already_Stored()
    {
        RespondWithProfile(DateTime.Today.ToString("yyyy-MM-dd"));
        var saves = MockSaveEndpoint();

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should()
                .Contain("Date of birth must be a date before today.");
            SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        }, WaitCeiling);

        saves.Should().BeEmpty();
    }

    [Fact]
    public void Maps_Backend_Date_Of_Birth_Rejection_Onto_The_Field()
    {
        RespondWithProfile("1989-06-15");
        var saves = MockSaveEndpoint(
            HttpStatusCode.BadRequest,
            """
            {
              "title": "The request is not valid.",
              "detail": "Date of birth must be a date before today.",
              "code": "date_of_birth_invalid"
            }
            """);

        var cut = RenderComponent<HealthProfilePage>();
        WaitForForm(cut);
        SaveButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            saves.Should().HaveCount(1);
            cut.Markup.Should()
                .Contain("Date of birth must be a date before today.");
        }, WaitCeiling);

        // NN/g rule 5: the problem is named next to the field, never as a
        // generic "could not be saved" alert.
        cut.Markup.Should().NotContain("Profile could not be saved.");
    }

    [Fact]
    public void Saves_A_Real_Date_Of_Birth()
    {
        RespondWithProfile("1989-06-15");
        var saves = MockSaveEndpoint();

        var cut = RenderComponent<HealthProfilePage>();
        PickDate(cut, new DateTime(1990, 1, 1));

        cut.WaitForAssertion(
            () => SaveButton(cut).HasAttribute("disabled")
                .Should().BeFalse(),
            WaitCeiling);

        SaveButton(cut).Click();

        cut.WaitForAssertion(() =>
        {
            saves.Should().HaveCount(1);
            saves[0].Should().Contain("1990-01-01");
        }, WaitCeiling);

        cut.Markup.Should().NotContain("Date of birth must be");
    }

    // --- Date of birth: the text the user types (t_85f07bff) --------------
    //
    // The field is editable, so typed text is the real risk: MudBlazor's
    // default converter dropped anything it could not parse, the page saved
    // with "Profile saved." and the DB kept DateOfBirth = NULL.

    [Fact]
    public async Task Typed_Date_Of_Birth_Is_Saved_Instead_Of_Dropped()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "1990-05-15");

        await Until(() =>
        {
            DateOfBirthMessage(cut).Should().BeEmpty();
            SaveButton(cut).HasAttribute("disabled").Should().BeFalse();
        });

        SaveButton(cut).Click();

        await Until(() => bodies.Should().HaveCount(1));
        bodies[0].Should().Contain("\"dateOfBirth\":\"1990-05-15\"");
    }

    [Fact]
    public async Task Typed_Date_Of_Birth_In_The_Usual_Locale_Format_Is_Saved()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "15/05/1990");

        await Until(() => SaveButton(cut).HasAttribute("disabled")
            .Should().BeFalse());

        SaveButton(cut).Click();

        await Until(() => bodies.Should().HaveCount(1));
        bodies[0].Should().Contain("\"dateOfBirth\":\"1990-05-15\"");
    }

    [Fact]
    public async Task Invalid_Date_Of_Birth_Explain_Itself_And_Blocks_Save()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "garbage");

        await Until(() =>
        {
            // The typed text stays so the user can correct it.
            DateOfBirthInput(cut).GetAttribute("value")
                .Should().Be("garbage");
            // The problem is named next to the field, not in a generic toast.
            DateOfBirthMessage(cut).Should().Contain("Enter a real date");
            DateOfBirthInput(cut).GetAttribute("aria-invalid")
                .Should().Be("true");
            SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        }, () => PickerHtml(cut));

        SaveButton(cut).Click();

        bodies.Should().BeEmpty();
        cut.Markup.Should().NotContain("Profile saved.");
    }

    // A date without a year is not a date. A lenient parse completes the
    // missing part from today, which used to save today's date silently and
    // produce an age-0 profile with corrupted blood-test ranges.
    [Theory]
    [InlineData("10/16")]
    [InlineData("15/05")]
    [InlineData("May 15")]
    [InlineData("1990")]
    [InlineData("15/05/90")]
    public async Task Incomplete_Date_Of_Birth_Is_Refused(string typed)
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, typed);

        await Until(() =>
        {
            DateOfBirthMessage(cut).Should().Contain("Enter a real date");
            SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        }, () => PickerHtml(cut));

        SaveButton(cut).Click();

        bodies.Should().BeEmpty();
        cut.Markup.Should().NotContain("Profile saved.");
    }

    [Fact]
    public async Task Future_Date_Of_Birth_Is_Refused_With_A_Specific_Message()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "2099-01-01");

        await Until(() =>
        {
            DateOfBirthMessage(cut)
                .Should().Contain("must be a date before today");
            SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        }, () => PickerHtml(cut));

        bodies.Should().BeEmpty();
        cut.Markup.Should().NotContain("Profile saved.");
    }

    // The form must not accept a date the backend would refuse: that used to
    // end in the generic "Profile could not be saved." alert.
    [Fact]
    public async Task Date_Of_Birth_Before_1900_Is_Refused_On_The_Field()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "1800-01-01");

        await Until(() =>
        {
            DateOfBirthMessage(cut)
                .Should().Contain("1900-01-01 or later");
            SaveButton(cut).HasAttribute("disabled").Should().BeTrue();
        }, () => PickerHtml(cut));

        SaveButton(cut).Click();

        bodies.Should().BeEmpty();
        cut.Markup.Should().NotContain("Profile could not be saved.");
    }

    [Fact]
    public async Task Error_Clears_Once_The_Date_Is_Corrected()
    {
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "garbage");
        await Until(() => DateOfBirthMessage(cut)
            .Should().Contain("Enter a real date"), () => PickerHtml(cut));

        await TypeDateOfBirthAsync(cut, "1990-05-15");

        await Until(() =>
        {
            DateOfBirthMessage(cut).Should().BeEmpty();
            SaveButton(cut).HasAttribute("disabled").Should().BeFalse();
        });
    }

    [Fact]
    public async Task Empty_Date_Of_Birth_Stay_A_Allowed_Option()
    {
        var bodies = MockSaveEndpoint();
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", EmptyProfile);

        var cut = RenderComponent<HealthProfilePage>();
        await Until(() => DateOfBirthInput(cut).GetAttribute("value")
            .Should().BeNullOrEmpty());

        await TypeDateOfBirthAsync(cut, "garbage");
        await Until(() => SaveButton(cut).HasAttribute("disabled")
            .Should().BeTrue());

        await TypeDateOfBirthAsync(cut, string.Empty);

        await Until(() =>
        {
            DateOfBirthMessage(cut).Should().BeEmpty();
            SaveButton(cut).HasAttribute("disabled").Should().BeFalse();
        });

        SaveButton(cut).Click();

        await Until(() => bodies.Should().HaveCount(1));
        bodies[0].Should().Contain("\"dateOfBirth\":null");
    }

    private void RespondWithProfile(string dateOfBirth)
        => MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", $$"""
                {
                  "name": "David",
                  "dateOfBirth": "{{dateOfBirth}}",
                  "biologicalSex": 1,
                  "heightCm": 180,
                  "weightKg": 80
                }
                """);

    private List<string> MockSaveEndpoint(
        HttpStatusCode status = HttpStatusCode.OK,
        string responseJson = """
            {
              "name": "David",
              "dateOfBirth": "1989-06-15",
              "biologicalSex": 1,
              "heightCm": 180,
              "weightKg": 80
            }
            """)
    {
        var bodies = new List<string>();

        MockHttp.When(HttpMethod.Put, "/HealthProfile")
            .Respond(async request =>
            {
                bodies.Add(await request.Content.ReadAsStringAsync());

                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(
                        responseJson,
                        Encoding.UTF8,
                        "application/json")
                };
            });

        return bodies;
    }

    private static void WaitForForm(IRenderedFragment cut)
        => cut.WaitForState(
            () => !cut.Markup.Contains("Loading profile"),
            WaitCeiling);

    // The picker raises DateChanged once the user has picked a date from the
    // calendar: that is the page's contract with the control for a value that
    // never went through the converter.
    private static void PickDate(
        IRenderedFragment cut,
        DateTime date)
    {
        WaitForForm(cut);

        var picker = cut.FindComponent<MudDatePicker>();
        picker.InvokeAsync(
            () => picker.Instance.DateChanged.InvokeAsync(date))
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Types into the field and leaves it: the picker validates when the user
    /// leaves the field, never while they are still typing.
    /// </summary>
    private static async Task TypeDateOfBirthAsync(
        IRenderedFragment cut, string text)
    {
        // MudDatePicker ignores a SetDate that arrives within 100ms of the
        // previous one (MudBlazor #7866), and the profile load sets the date
        // when the field appears. Typing inside that window is silently
        // dropped, so the steps stay outside it.
        await Task.Delay(150);

        // Re-find the input before each step: a re-render can replace the
        // element, and an event on a detached element is silently ignored.
        DateOfBirthInput(cut).Change(text);
        await Task.Delay(150);
        DateOfBirthInput(cut).Blur();
        await Task.Delay(150);
    }

    /// <summary>
    /// Runs an assertion until it passes, retrying every 50ms for up to 5s.
    /// The picker validates asynchronously (change -> convert -> validate ->
    /// render), and each retry yields so the test's synchronization context can
    /// run that pending work.
    /// </summary>
    private static async Task Until(
        Action assertion, Func<string>? describe = null)
    {
        Exception? failure = null;

        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            await Task.Delay(50);
        }

        throw new Exception(
            $"{failure?.Message}\nfield html: {describe?.Invoke()}",
            failure);
    }

    private static string PickerHtml(IRenderedFragment cut)
        => cut.Find(".mud-picker").OuterHtml;

    private static IElement DateOfBirthInput(IRenderedFragment cut)
        => cut.Find(".mud-picker input");

    private static string DateOfBirthMessage(IRenderedFragment cut)
        => string.Join(" ", cut
            .FindAll(".mud-picker .mud-input-control-helper-container")
            .Select(element => element.TextContent))
            .Trim();

    private static IElement SaveButton(IRenderedFragment cut)
        => cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Save"));
}
