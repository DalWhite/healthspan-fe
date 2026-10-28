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

        cut.Find("[role='status']").TextContent.Should().Contain("Cargando");
    }

    [Fact]
    public void ThemeState_Is_Dark_Only_And_Notifies_Only_On_An_Actual_Change()
    {
        var themeState = new ThemeState();
        var notifications = 0;
        themeState.Changed += () => notifications++;

        // DESIGN.md §6: dark-only by default, so re-asserting dark is a no-op.
        themeState.IsDarkMode.Should().BeTrue();
        themeState.SetDarkMode(true);
        themeState.SetDarkMode(true);
        notifications.Should().Be(0);

        themeState.SetDarkMode(false);
        notifications.Should().Be(1);
    }

    [Fact]
    public void Theme_Uses_The_Design_System_V1_Palette_And_Inter()
    {
        var palette = HealthspanTheme.Default.PaletteDark;

        // MudColor normalizes to lowercase #RRGGBBAA.
        palette.Primary.Value.Should().Be("#1e6fffff");
        palette.Secondary.Value.Should().Be("#1e6fffff");
        palette.Background.Value.Should().Be("#0e1116ff");
        palette.Surface.Value.Should().Be("#161b22ff");
        palette.DrawerBackground.Value.Should().Be("#161b22ff");
        palette.AppbarBackground.Value.Should().Be("#161b22ff");
        palette.LinesDefault.Value.Should().Be("#262c36ff");
        palette.Success.Value.Should().Be("#2fbf71ff");
        palette.Error.Value.Should().Be("#f1545bff");

        // DESIGN.md §2: Inter replaces MudBlazor's Roboto default.
        HealthspanTheme.Default.Typography.Default.FontFamily.Should().Contain("Inter");
        HealthspanTheme.Default.Typography.Body1.FontFamily.Should().Contain("Inter");
        HealthspanTheme.Default.Typography.Button.FontFamily.Should().Contain("Inter");
    }

    [Fact]
    public void Stylesheet_Keeps_The_Design_System_V1_Token_Contract()
    {
        var css = RepoFile(Path.Combine("wwwroot", "css", "app.css"));
        var index = RepoFile(Path.Combine("wwwroot", "index.html"));

        foreach (var token in new[]
                 {
                     "--hs-canvas: #0E1116", "--hs-card: #161B22", "--hs-accent: #1E6FFF",
                     "--hs-done: #2FBF71", "--hs-pr: #F5B83D", "--hs-err: #F1545B",
                     "--hs-t1: #F2F4F8", "--hs-t2: #A9B1BE", "--hs-t3: #78818F"
                 })
        {
            css.Should().Contain(token, $"{token} is a v1 token the screens build on");
        }

        // DESIGN.md §2: tabular numbers globally, §3: the shared component classes.
        css.Should().Contain("font-variant-numeric: tabular-nums");
        foreach (var componentClass in new[] { ".hs-chip", ".hs-settable", ".hs-fab", ".hs-hero" })
        {
            css.Should().Contain(componentClass);
        }

        // No drop shadows in v1 (DESIGN.md §1): depth comes from the surface ladder.
        css.Should().Contain("--hs-shadow-card: none");
        css.Should().Contain("--hs-shadow-panel: none");

        // Single accent: MudBlazor's default indigo must not survive anywhere.
        var cssLower = css.ToLowerInvariant();
        cssLower.Should().NotContain("#665ac1", "the default MudBlazor accent is a second actionable colour");
        cssLower.Should().NotContain("#594ae2", "the default MudBlazor accent is a second actionable colour");

        // DESIGN.md §6: the legacy teal brand gradient is off-system (login panels use canvas + accent wash).
        cssLower.Should().NotContain("#4d9f95", "legacy teal brand colour is not part of the v1 palette");
        cssLower.Should().NotContain("#1a3c3b", "legacy teal brand colour is not part of the v1 palette");

        // QA t_d99f727c F3: the whole legacy teal family (health components) is off-system too.
        foreach (var legacy in new[]
                 {
                     "#0f5f67", "#176d74", "#133f44", "#0b7d86", "#17373c",
                     "#f5fbfb", "#f4fbfb", "#eef4f4", "#fbfdfd", "#f4f8f8",
                     "#0d5a61", "#103f45", "#ffa000",
                     "rgba(15, 95, 103", "rgba(22, 48, 54", "rgba(8, 45, 49",
                     "rgba(12, 56, 60", "rgba(245, 251, 251", "rgba(244, 248, 248"
                 })
        {
            cssLower.Should().NotContain(legacy, "legacy teal/amber value is not part of the v1 palette");
        }

        // QA t_d99f727c F1: a focused page title gets the accent ring, never the UA amber ring.
        css.Should().Contain(".hs-page-title:focus-visible", "focused page titles must not fall back to the browser outline");

        // DESIGN.md §2: Inter is loaded by the app shell.
        index.Should().Contain("family=Inter");
        index.ToLowerInvariant().Should().NotContain("family=roboto", "Roboto is not the v1 typeface");
    }

    // QA t_097d7195 D2 / WCAG 2.4.7: MudBlazor ships `button:focus{outline:none}` (0,1,1) and
    // `.mud-button-filled:focus-visible{box-shadow:<elevation>}` (0,2,0), which both outrank the
    // zero-specificity `:where(...)` ring, so no button in the app showed keyboard focus.
    [Fact]
    public void Focus_Ring_Outranks_MudBlazor_Resets_On_Buttons_And_Fields()
    {
        var css = RepoFile(Path.Combine("wwwroot", "css", "app.css"));

        var buttons = CssBlock(css, ".mud-button-root:focus-visible");
        buttons.Should().Contain("outline: 2px solid var(--mud-palette-primary)",
            "the ring must be restated at class specificity to beat MudBlazor's button:focus reset");
        buttons.Should().Contain("box-shadow: var(--hs-shadow-focus)",
            "filled buttons keep their resting elevation unless the halo is restated too");

        var fields = CssBlock(css, ".mud-input:has(input.mud-input-slot:focus-visible) .mud-input-outlined-border");
        fields.Should().Contain("border-width: 2px");
        fields.Should().Contain("box-shadow: var(--hs-shadow-focus)",
            "the outlined container carries the halo, not the inner input");

        css.Should().NotContain(".mud-input-slot:focus-visible {",
            "the old rule cancelled the inner halo while nothing replaced it outside (QA measured changed=false)");
    }

    private static string CssBlock(string css, string selector)
    {
        var at = css.IndexOf(selector, StringComparison.Ordinal);
        if (at < 0)
        {
            throw new InvalidOperationException($"CSS rule '{selector}' not found in app.css.");
        }

        // el selector puede encabezar una lista: el bloque empieza en la primera '{' tras él
        var open = css.IndexOf('{', at);
        var close = open < 0 ? -1 : css.IndexOf('}', open);
        if (open < 0 || close < 0)
        {
            throw new InvalidOperationException($"CSS rule '{selector}' has no body.");
        }

        return css[open..close];
    }

    private static string RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"{relative} was not found above the test assembly.");
    }
}
