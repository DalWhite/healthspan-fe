using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// QA prod audit 390x844 (t_1d3b6d14): MudBlazor's default snackbar close (X) is a
/// MudIconButton Size.Small — 26x26 px (under the 44px touch target) — rendered with
/// no accessible name. The app disables it globally (MudAppConfig); snackbars
/// auto-close instead. Guards assert the config AND the rendered DOM so a future
/// MudBlazor upgrade cannot silently bring the X back.
/// </summary>
public class SnackbarA11yTests : BlazorTestBase
{
    [Fact]
    public void Snackbar_Configuration_Hides_Close_Icon_And_Keeps_Auto_Close()
    {
        var snackbar = Services.GetRequiredService<ISnackbar>();

        snackbar.Configuration.ShowCloseIcon.Should().BeFalse(
            "the default 26x26 close X is under the 44px touch target and has no accessible name");
        snackbar.Configuration.VisibleStateDuration.Should().BeGreaterThan(0,
            "auto-close is the dismissal path that replaces the close button");
    }

    [Fact]
    public void Rendered_Snackbar_Has_No_Close_Button()
    {
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var cut = RenderComponent<MudSnackbarProvider>();

        snackbar.Add("Ejercicio añadido.", Severity.Success);
        cut.WaitForElement(".mud-snackbar");

        var bar = cut.Find(".mud-snackbar");
        bar.QuerySelectorAll("button").Should().BeEmpty(
            "no interactive control may sit inside the snackbar below 44px or without a name");
        bar.QuerySelectorAll(".mud-icon-button").Should().BeEmpty();
    }
}
