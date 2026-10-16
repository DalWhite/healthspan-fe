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

    // --- Date of birth inline validation (t_343b882e) ---------------------

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

    private static void WaitForForm(IRenderedComponent<HealthProfilePage> cut)
        => cut.WaitForState(
            () => !cut.Markup.Contains("Loading profile"),
            WaitCeiling);

    // The picker raises DateChanged once the user has picked or typed a
    // complete date: that is the page's contract with the control.
    private static void PickDate(
        IRenderedComponent<HealthProfilePage> cut,
        DateTime date)
    {
        WaitForForm(cut);

        var picker = cut.FindComponent<MudDatePicker>();
        picker.InvokeAsync(
            () => picker.Instance.DateChanged.InvokeAsync(date))
            .GetAwaiter().GetResult();
    }

    private static IElement SaveButton(
        IRenderedComponent<HealthProfilePage> cut)
        => cut.FindAll("button")
            .First(button => button.TextContent.Contains("Save"));
}
