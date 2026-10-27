using System.Net.Http;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using RichardSzalay.MockHttp;
using Xunit;
using HealthProfilePage =
    healthspan_fe_wasm_pwa.Pages.Health.HealthProfile;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WCAG 1.3.1/3.3.2 regression (a11y run7 / card t_05cb3b17): every input of
/// /health/profile must have an accessible name through label[for] -> input[id].
/// MudBlazor 6.21 only wires that when an explicit `id` is splatted onto the
/// input component (FieldId -> label `for`, id -> the rendered &lt;input&gt;).
/// Before the fix the label's `for` pointed at a generated "mudinput-*" id that
/// never reached the DOM, so Name, Date of birth, Height (cm) and Current
/// weight (kg) were named only by their placeholder.
/// </summary>
public sealed class HealthProfileLabelWiringTests : BlazorTestBase
{
    private static readonly TimeSpan WaitCeiling = TimeSpan.FromSeconds(20);

    private const string FilledProfile = """
        {
          "name": "David",
          "dateOfBirth": "1989-06-15",
          "biologicalSex": 0,
          "heightCm": 178,
          "weightKg": 74
        }
        """;

    [Fact]
    public void Personal_Profile_Inputs_Are_LabelWired()
    {
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", FilledProfile);

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
            cut.FindAll("input#health-profile-name").Should().HaveCount(1),
            WaitCeiling);

        AssertLabelWired(cut, "health-profile-name", "Name");
        AssertLabelWired(cut, "health-profile-date-of-birth", "Date of birth");
        AssertLabelWired(cut, "health-profile-height-cm", "Height (cm)");
        AssertLabelWired(cut, "health-profile-weight-kg", "Current weight (kg)");
    }

    [Fact]
    public void Biological_Sex_Select_Is_Label_Wired()
    {
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", FilledProfile);

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
            cut.FindAll("input#health-profile-biological-sex").Should().HaveCount(1),
            WaitCeiling);

        AssertLabelWired(cut, "health-profile-biological-sex", "Biological sex");
    }

    [Fact]
    public void Profile_Field_Ids_Are_Unique()
    {
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", FilledProfile);

        var cut = RenderComponent<HealthProfilePage>();

        cut.WaitForAssertion(() =>
            cut.FindAll("input#health-profile-name").Should().HaveCount(1),
            WaitCeiling);

        var duplicateIds = cut.FindAll("[id]")
            .Select(element => element.Id)
            .GroupBy(id => id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        duplicateIds.Should().BeEmpty("ids must stay unique per page");
    }

    private static void AssertLabelWired(
        IRenderedFragment cut, string id, string expectedLabel)
    {
        var input = cut.Find($"input#{id}");
        input.Should().NotBeNull($"input#{id} must exist (id must land on the <input>)");

        var label = cut.Find($"label[for=\"{id}\"]");
        label.TextContent.Trim().Should().Be(expectedLabel,
            $"label[for=\"{id}\"] must be the visible label for that input");
    }
}
