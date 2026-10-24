using MudBlazor;
using MudBlazor.Services;

namespace healthspan_fe_wasm_pwa.Shared;

/// <summary>
/// Single source of truth for MudBlazor service configuration, shared by
/// Program.cs and the bUnit test base so guards assert what the app runs.
/// </summary>
public static class MudAppConfig
{
    public static void Configure(MudServicesConfiguration config)
    {
        // QA a11y audit 390x844 (t_1d3b6d14): the default snackbar close (X) is a
        // MudIconButton Size.Small — 26x26 px, under the 44px touch target — with no
        // accessible name, so it is also unusable by screen readers. Snackbars
        // auto-close (VisibleStateDuration 5s) and pause on hover/touch, so the
        // close button adds no value: remove it (subtract > add).
        config.SnackbarConfiguration.ShowCloseIcon = false;
    }
}
