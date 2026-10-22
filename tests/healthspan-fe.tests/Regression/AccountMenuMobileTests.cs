using System;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The topbar account menu is the only way to reach the health sections, and on
/// phone widths it behaved as a desktop dropdown: MudBlazor positioned the popover
/// with inline left/top against the document (so it drifted away from the sticky
/// topbar as soon as the page scrolled), its click-away overlay was trapped inside
/// the topbar because backdrop-filter makes the bar the containing block for
/// position:fixed descendants, and the bar ignored the PWA safe area, which on a
/// notched phone in standalone mode puts the 44px avatar button entirely under the
/// status bar. The menu is now a bottom sheet pinned above the nav, dismissed by a
/// viewport-wide scrim, with a named activator button.
/// </summary>
public class AccountMenuMobileTests : BlazorTestBase
{
    private const string SheetClass = "hs-account-sheet";

    /// <summary>
    /// MudBlazor renders popover content into a popover provider, which the app
    /// mounts next to the layout; bUnit needs the same shape or the sheet body
    /// never renders.
    /// </summary>
    private IRenderedFragment RenderMainLayout()
        => Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MainLayout>(1);
            builder.CloseComponent();
        });

    [Fact]
    public void Topbar_Account_Menu_Carries_The_Bottom_Sheet_Popover_Class()
    {
        var cut = RenderMainLayout();

        var menu = cut.FindComponent<MudMenu>();
        menu.Instance.PopoverClass.Should().Be(SheetClass,
            "the mobile sheet rules hang off the popover class; without it the menu falls back to the desktop dropdown");

        var button = cut.Find(".hs-topbar-account button");
        button.GetAttribute("aria-label").Should().Be("Account and health sections",
            "the account button is icon-only; MudMenu has no AriaLabel parameter so the name must sit on the button itself");
    }

    [Fact]
    public void Account_Sheet_Rows_Navigate_By_Href_Not_By_OnClick()
    {
        var razor = ReadMainLayoutRazor();
        var sheet = razor[razor.IndexOf("hs-topbar-account", StringComparison.Ordinal)..];

        var rows = Regex.Matches(sheet, @"<MudMenuItem[^>]*>");
        rows.Count.Should().BeGreaterThanOrEqualTo(6,
            "the account sheet is the only route to the health sections; every section must appear as a row");

        var hrefs = rows.Select(m => Regex.Match(m.Value, "Href=\"([^\"]+)\"").Groups[1].Value).ToList();
        foreach (var required in new[] { "/health/dashboard", "/health/blood-tests", "/health/biomarkers", "/health/assessments", "/health/profile", "/settings", "/logout" })
        {
            hrefs.Should().Contain(required,
                "below 1000px the drawer is gone, so the account sheet is the only way to reach " + required);
        }

        foreach (Match row in rows)
        {
            var tag = row.Value;
            tag.Should().Contain("Href=\"/",
                "MudMenuItem handles a tap through its touch path (@ontouchend), and that path honours only Href or OnTouch — "
                + "an OnClick-only row closes the sheet on a phone without ever navigating");
            tag.Should().NotContain("OnClick",
                "OnClick is the desktop path; on a phone it silently swallows the tap");
        }
    }

    [Fact]
    public void Tapping_The_Scrim_Outside_The_Sheet_Dismisses_It()
    {
        var cut = RenderMainLayout();
        var menu = cut.FindComponent<MudMenu>();

        cut.Find(".hs-topbar-account .mud-menu-activator").Click();
        cut.WaitForState(() => menu.Instance.IsOpen, TimeSpan.FromSeconds(5));

        cut.Find(".hs-topbar-account .mud-overlay").Click();
        cut.WaitForState(() => !menu.Instance.IsOpen, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Stylesheet_Keeps_The_Topbar_Clear_Of_The_Pwa_Status_Bar()
    {
        var css = ReadAppCss();

        var topbar = RuleBlock(css, ".hs-topbar");
        topbar.Should().Contain("env(safe-area-inset-top)",
            "the app is installed as a standalone PWA with viewport-fit=cover; without the inset the avatar sits under the status bar on notched phones");
        topbar.Should().NotContain("backdrop-filter",
            "backdrop-filter on the bar itself makes it the containing block for position:fixed descendants and traps MudBlazor's click-away overlay inside the 52px bar");

        var frost = RuleBlock(css, ".hs-topbar::before");
        frost.Should().Contain("backdrop-filter: blur(12px)",
            "the frosted bar must stay, just on a child layer that cannot trap fixed descendants");
    }

    [Fact]
    public void Stylesheet_Pins_The_Account_Sheet_Above_The_Nav_With_A_Dismissing_Scrim()
    {
        var css = ReadAppCss();

        var sheet = RuleBlock(css, ".hs-account-sheet.mud-popover");
        sheet.Should().Contain("position: fixed !important",
            "MudBlazor positions the popover with inline left/top against the document, so only an override pins it");
        sheet.Should().Contain("top: auto !important");
        sheet.Should().Contain("bottom: calc(var(--hs-bottom-nav-height) + env(safe-area-inset-bottom)",
            "the sheet must clear the bottom nav and the home indicator");
        sheet.Should().Contain("max-width: none !important",
            "the sheet is full width on a phone, not a 178px desktop dropdown");

        var paper = RuleBlock(css, ".hs-account-sheet .mud-menu-paper");
        paper.Should().Contain("width: 100%");

        var row = RuleBlock(css, ".hs-account-sheet .mud-list-item");
        row.Should().Contain("min-height: var(--hs-touch-target)",
            "sheet rows are thumb targets, not desktop menu rows");

        var scrimZ = ZIndex(RuleBlock(css, ".hs-topbar-account .mud-overlay"));
        var sheetZ = ZIndex(sheet);
        var navZ = ZIndex(RuleBlock(css, ".hs-bottom-nav"));
        var barZ = ZIndex(RuleBlock(css, ".hs-topbar"));

        scrimZ.Should().BeGreaterThan(navZ,
            "taps anywhere on the screen — including the bottom nav — must dismiss the sheet");
        sheetZ.Should().BeGreaterThan(scrimZ,
            "the sheet itself must stay above the scrim so its rows are tappable");
        barZ.Should().BeGreaterThan(scrimZ,
            "the scrim lives inside the topbar's stacking context, so the bar must outrank the nav for the scrim to cover it");
    }

    [Fact]
    public void Sheet_Rules_Are_Scoped_To_Phone_Widths()
    {
        var css = ReadAppCss();

        var media = MediaHeader(css, ".hs-account-sheet.mud-popover");
        media.Should().NotBeNull();
        media.Should().MatchRegex(@"max-width:\s*999px",
            "the sheet is the phone layout; at >=1000px the app has a sidebar and the menu stays a dropdown");
    }

    // ── helpers ────────────────────────────────────────────────────────────

    /// <summary>Body of the first rule whose selector matches exactly.</summary>
    private static string RuleBlock(string css, string selector)
    {
        var match = Regex.Match(css, Regex.Escape(selector) + @"\s*\{");
        if (!match.Success)
        {
            throw new InvalidOperationException($"CSS rule '{selector}' not found in app.css.");
        }

        var depth = 1;
        var start = match.Index + match.Length;
        for (var i = start; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css[start..i];
                }
            }
        }

        throw new InvalidOperationException($"Unterminated CSS rule for '{selector}'.");
    }

    /// <summary>The @media header guarding the first rule that uses the selector.</summary>
    private static string? MediaHeader(string css, string selector)
    {
        var at = css.IndexOf(selector, StringComparison.Ordinal);
        if (at < 0)
        {
            return null;
        }

        var open = css.LastIndexOf("@media", at, StringComparison.Ordinal);
        if (open < 0)
        {
            return null;
        }

        var brace = css.IndexOf('{', open);
        return brace < 0 ? null : css[open..brace];
    }

    private static int ZIndex(string ruleBody)
    {
        var match = Regex.Match(ruleBody, @"z-index:\s*(\d+)");
        match.Success.Should().BeTrue("the rule must declare an explicit z-index");
        return int.Parse(match.Groups[1].Value);
    }

    private static string ReadMainLayoutRazor()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Shared", "MainLayout.razor");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Shared/MainLayout.razor was not found above the test assembly.");
    }

    private static string ReadAppCss()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "wwwroot", "css", "app.css");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("wwwroot/css/app.css was not found above the test assembly.");
    }
}
