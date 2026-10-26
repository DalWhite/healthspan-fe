namespace healthspan_fe_wasm_pwa.Shared;

// Shared vocabulary for the unauthenticated journey (spec
// docs/design/unauth-redirect-state.md §5): one destination→name table used
// by the bridge (RedirectToLogin) and the contextual login/register pages,
// plus the single returnUrl validation rule (safe internal path, fallback
// /training/today) so login and register agree on the same contract.
public static class ProtectedViews
{
    public const string DefaultReturnUrl = "/training/today";

    private static readonly (string Prefix, string Name)[] Destinations =
    [
        ("/training/today", "tu entrenamiento de hoy"),
        ("/training/history", "tu historial de entrenamientos"),
        ("/training/insights", "tus insights"),
        ("/training/library", "tu biblioteca"),
        ("/wellbeing/activities", "tus actividades de bienestar"),
        ("/health/biomarkers", "tus biomarcadores"),
        ("/settings", "tus ajustes"),
        ("/premium", "tu plan Premium"),
    ];

    // §5 table: prefix match over the return URL (query ignored); unknown
    // protected routes fall back to "tu Healthspan".
    public static string DestinationName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "tu Healthspan";
        }

        var path = url.Split('?', '#')[0];

        foreach (var (prefix, name) in Destinations)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return "tu Healthspan";
    }

    // Chip label: "Vista protegida · Entrenamiento de hoy" (§6) — the phrase
    // without the possessive, title-cased on the first word.
    public static string DestinationChipName(string? url)
    {
        var name = DestinationName(url);
        const string possessive = "tu ";

        if (name.StartsWith(possessive, StringComparison.OrdinalIgnoreCase))
        {
            name = name[possessive.Length..];
        }

        return name.Length > 0
            ? char.ToUpperInvariant(name[0]) + name[1..]
            : name;
    }

    // Same validation GetSafeReturnUrl measured OK in the audit: internal
    // path only, never a protocol-relative or absolute URL.
    public static bool IsSafeReturnUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        url.StartsWith('/') &&
        !url.StartsWith("//", StringComparison.Ordinal) &&
        !url.StartsWith("/\\", StringComparison.Ordinal);

    public static string SafeReturnUrl(string? url) =>
        IsSafeReturnUrl(url) ? url! : DefaultReturnUrl;

    public static string LoginUrlFor(string? returnUrl) =>
        $"/login?returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}";
}
