using System.Globalization;

/// <summary>
/// FE mirror of the server-owned Premium catalog (docs/billing-api-t_c0e8a770.md
/// §Plans: `premium-monthly` 999 ct EUR/month, `premium-annual` 8999 ct EUR/year).
/// There is no GET /billing/plans endpoint in the Fase 1 contract, so the FE
/// mirrors the seeded slugs; checkout validates the slug server-side
/// (404 plan_not_found if the catalog ever drifts).
/// </summary>
public sealed record PremiumPlan(
    string Slug,
    string Name,
    int PriceCents,
    string Currency,
    string Interval,
    string? SaveBadge)
{
    public string PriceLabel => FormatPrice(PriceCents, Currency);

    public static string FormatPrice(int cents, string currency)
    {
        var value = cents / 100m;
        var culture = CultureInfo.GetCultureInfo("es-ES");
        var formatted = value.ToString("0.00", culture);
        return currency.ToUpperInvariant() switch
        {
            "EUR" => $"{formatted} €",
            "USD" => "$ {formatted}",
            _ => $"{formatted} {currency}",
        };
    }
}

public static class PremiumPlans
{
    // Mensual preseleccionado (spec D3: el anual se gana con badge, no por defecto).
    public static readonly PremiumPlan Monthly =
        new("premium-monthly", "Mensual", 999, "EUR", "mes", null);

    public static readonly PremiumPlan Annual =
        new("premium-annual", "Anual", 8999, "EUR", "año", "2 meses gratis");

    public static readonly IReadOnlyList<PremiumPlan> All = new[] { Monthly, Annual };

    public static PremiumPlan? Find(string? slug)
        => All.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
