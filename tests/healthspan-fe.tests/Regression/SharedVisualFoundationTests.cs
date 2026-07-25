using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Shared;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class SharedVisualFoundationTests : BlazorTestBase
{
    [Fact]
    public void PageHeader_Marks_TitleOnly_Headers_For_Compact_Mobile_Rendering()
    {
        var cut = RenderComponent<PageHeader>(parameters => parameters
            .Add(component => component.Title, "Training History"));

        cut.Find("header").ClassList.Should().Contain("hs-page-header-title-only");
        cut.Find("h1").TextContent.Should().Be("Training History");
    }

    [Fact]
    public void PageShell_Composes_Responsive_Width_Classes()
    {
        var cut = RenderComponent<PageShell>(parameters => parameters
            .Add(component => component.Narrow, true)
            .Add(component => component.Class, "test-page")
            .AddChildContent("Content"));

        cut.Find(".hs-page-shell")
            .ClassList
            .Should()
            .Contain(new[] { "hs-page-shell-narrow", "test-page" });
    }

    [Fact]
    public void PageToolbar_Exposes_An_Accessible_Label()
    {
        var cut = RenderComponent<PageToolbar>(parameters => parameters
            .Add(component => component.AriaLabel, "History filters")
            .AddChildContent("Controls"));

        var toolbar = cut.Find("[role='region']");
        toolbar.GetAttribute("aria-label").Should().Be("History filters");
    }

    [Fact]
    public void ErrorState_Announces_The_Error_And_Invokes_Its_Action()
    {
        var actionInvoked = false;
        var cut = RenderComponent<ErrorState>(parameters => parameters
            .Add(component => component.Title, "Could not load")
            .Add(component => component.Message, "Try again.")
            .Add(component => component.ActionLabel, "Retry")
            .Add(component => component.OnAction, () => actionInvoked = true));

        cut.Find("[role='alert']").TextContent.Should().Contain("Could not load");
        cut.Find("button").Click();
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public void LoadingState_Always_Has_An_Announced_Label()
    {
        var cut = RenderComponent<LoadingState>();

        cut.Find("[role='status']").TextContent.Should().Contain("Loading");
    }

    [Fact]
    public void ThemeState_Notifies_Only_When_The_Mode_Changes()
    {
        var themeState = new ThemeState();
        var notifications = 0;
        themeState.Changed += () => notifications++;

        themeState.SetDarkMode(true);
        themeState.SetDarkMode(true);

        themeState.IsDarkMode.Should().BeTrue();
        notifications.Should().Be(1);
        HealthspanTheme.Default.PaletteDark.Surface.Value.Should().Be("#172b2eff");
    }
}
