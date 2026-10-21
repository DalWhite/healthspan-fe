using MudBlazor;

namespace healthspan_fe_wasm_pwa.Shared;

/// <summary>
/// Design system v1 (docs/design/DESIGN.md): dark-only, single accent.
/// Both palettes carry the same dark values so the app stays dark regardless
/// of the IsDarkMode flag — the light toggle is a later option, not v1.
/// </summary>
public static class HealthspanTheme
{
    // DESIGN.md §1 color tokens.
    public const string Canvas = "#0E1116";
    public const string Card = "#161B22";
    public const string Input = "#1F2630";
    public const string Raised = "#283040";
    public const string Border = "#262C36";
    public const string Accent = "#1E6FFF";
    public const string Done = "#2FBF71";
    public const string Pr = "#F5B83D";
    public const string Err = "#F1545B";
    public const string Text1 = "#F2F4F8";
    public const string Text2 = "#A9B1BE";
    public const string Text3 = "#78818F";

    public static MudTheme Default { get; } = BuildTheme();

    private static MudTheme BuildTheme()
    {
        var light = new PaletteLight();
        var dark = new PaletteDark();
        ApplyV1Palette(light);
        ApplyV1Palette(dark);

        return new MudTheme
        {
            Palette = light,
            PaletteDark = dark,
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
            },
            // DESIGN.md §2: Inter everywhere, including MudBlazor's own
            // typography variants (they default to Roboto otherwise).
            Typography = BuildTypography(),
        };
    }

    private static Typography BuildTypography()
    {
        // DESIGN.md §2: Inter everywhere, including MudBlazor's own
        // typography variants (they default to Roboto otherwise).
        var interFamily = new[]
        {
            "Inter", "system-ui", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"
        };

        var typography = new Typography();
        foreach (var variant in new BaseTypography[]
                 {
                     typography.Default, typography.H1, typography.H2, typography.H3,
                     typography.H4, typography.H5, typography.H6, typography.Subtitle1,
                     typography.Subtitle2, typography.Body1, typography.Body2,
                     typography.Button, typography.Caption, typography.Overline
                 })
        {
            variant.FontFamily = interFamily;
        }

        return typography;
    }

    // PaletteLight and PaletteDark share only the (obsolete) Palette base in
    // MudBlazor 6.21; the v1 values are applied to both through it.
#pragma warning disable CS0618
    private static void ApplyV1Palette(Palette palette)
#pragma warning restore CS0618
    {
        palette.Primary = Accent;
        palette.PrimaryContrastText = Text1;
        // Single accent: MudBlazor's secondary role must not introduce a
        // second actionable color (DESIGN.md §6).
        palette.Secondary = Accent;
        palette.SecondaryContrastText = Text1;
        palette.Tertiary = Done;
        palette.Info = Accent;
        palette.InfoContrastText = Text1;
        palette.Success = Done;
        palette.Warning = Pr;
        palette.Error = Err;

        palette.Black = Canvas;
        palette.Background = Canvas;
        palette.BackgroundGrey = Card;
        palette.Surface = Card;
        palette.DrawerBackground = Card;
        palette.DrawerText = Text1;
        palette.DrawerIcon = Text2;
        palette.AppbarBackground = Card;
        palette.AppbarText = Text1;

        palette.TextPrimary = Text1;
        palette.TextSecondary = Text2;
        palette.TextDisabled = Text3;
        palette.ActionDefault = Text2;
        palette.ActionDisabled = Text3;
        palette.ActionDisabledBackground = Input;

        palette.LinesDefault = Border;
        palette.LinesInputs = Border;
        palette.TableLines = Border;
        palette.TableStriped = Input;
        palette.TableHover = Raised;
        palette.Divider = Border;
        palette.DividerLight = Border;
        palette.ChipDefault = Input;
        palette.ChipDefaultHover = Raised;
    }
}

public sealed class ThemeState
{
    // Dark-only by default (DESIGN.md §6).
    public bool IsDarkMode { get; private set; } = true;

    public event Action? Changed;

    public void SetDarkMode(bool isDarkMode)
    {
        if (IsDarkMode == isDarkMode)
        {
            return;
        }

        IsDarkMode = isDarkMode;
        Changed?.Invoke();
    }
}
