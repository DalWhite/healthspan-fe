using MudBlazor;

namespace healthspan_fe_wasm_pwa.Shared;

public static class HealthspanTheme
{
    public static MudTheme Default { get; } = new()
    {
        Palette = new PaletteLight
        {
            Primary = "#ae1d0d",
            Secondary = "#035960",
            AppbarBackground = "#082f33",
            DrawerBackground = "#f7faf9",
            Background = "#f3f7f6",
            Surface = "#ffffff"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#e65a49",
            Secondary = "#34bac4",
            AppbarBackground = "#102c30",
            DrawerBackground = "#13272a",
            Background = "#0e1c1f",
            Surface = "#172b2e"
        }
    };
}

public sealed class ThemeState
{
    public bool IsDarkMode { get; private set; }

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
