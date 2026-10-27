using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// WCAG 1.3.1/3.3.2/4.1.2 regression (a11y run7 / card t_bfd96732): every input on
/// /health/assessments must expose an accessible name. MudBlazor 6.21 wires
/// label[for] -> input[id] only when an explicit `id` is splatted onto the input
/// component (the id becomes both the &lt;input&gt; id and the label's `for`); without
/// it the label's `for` points at a generated "mudinput-*" id that never reaches the
/// DOM, so `el.labels` is empty and the field is reported as unlabeled.
/// The nine score fields share the visible label "Score", so each one also carries
/// aria-label "Score —&lt;adaptation&gt;" to give screen-reader users a distinct name
/// that still starts with the visible label text (WCAG 2.5.3).
/// </summary>
public sealed class AssessmentsLabelWiringTests : BlazorTestBase
{
    private const string CatalogJson =
        "[{\"adaptation\":0,\"displayName\":null,\"description\":null}," +
        "{\"adaptation\":3,\"displayName\":\"Strength\",\"description\":null}]";

    private IRenderedFragment RenderAssessments()
    {
        MockHttp.When("/GalpinAssessments/catalog")
            .Respond("application/json", CatalogJson);
        MockHttp.When("/GalpinAssessments")
            .Respond("application/json", "[]");
        MockHttp.When("/GalpinAssessments/trends/*")
            .Respond("application/json", "null");

        var cut = RenderComponent<Assessments>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("input#assessment-date").Should().HaveCount(1);
        }, timeout: TimeSpan.FromSeconds(5));

        return cut;
    }

    private static void AssertLabelWired(IRenderedFragment cut, string selector, string id, string expectedLabel)
    {
        cut.FindAll(selector).Should().HaveCount(1, $"{selector} must exist (id must land on the field)");
        var label = cut.Find($"label[for=\"{id}\"]");
        label.TextContent.Trim().Should().Be(expectedLabel,
            $"label[for=\"{id}\"] must be the visible label of that field");
    }

    [Fact]
    public void Date_And_Notes_Are_LabelWired()
    {
        var cut = RenderAssessments();

        AssertLabelWired(cut, "input#assessment-date", "assessment-date", "Assessed on");
        AssertLabelWired(cut, "textarea#assessment-notes", "assessment-notes", "Notes (optional)");
    }

    [Fact]
    public void Every_Score_Field_Has_A_Unique_Accessible_Name()
    {
        var cut = RenderAssessments();

        var names = cut.FindAll("input[id^=\"assessment-score-\"]")
            .Select(input =>
            {
                var id = input.Id;
                var ariaLabel = input.GetAttribute("aria-label");

                cut.Find($"label[for=\"{id}\"]").TextContent.Trim().Should().Be("Score",
                    $"visible label of #{id} stays \"Score\"");

                ariaLabel.Should().NotBeNullOrWhiteSpace(
                    $"#{id} needs a distinct accessible name (nine fields share the visible label \"Score\")");
                ariaLabel!.Should().StartWith("Score",
                    $"the accessible name must contain the visible label text (WCAG 2.5.3)");

                return ariaLabel;
            })
            .ToList();

        names.Should().HaveCount(2, "catalog has two adaptations");
        names.Should().OnlyHaveUniqueItems();
        names.Should().Contain("Score — Strength");
    }
}
