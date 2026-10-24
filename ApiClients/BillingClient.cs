using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

/// <summary>
/// Client for /api/billing and /api/trainers payment-link endpoints.
/// Contract: healthspan-api docs/billing-api-t_c0e8a770.md.
/// ProblemDetails errors are surfaced as <see cref="BillingApiException.Code"/>
/// (machine-readable `code` extension) so pages can render the exact UX state
/// (subscription_active, billing_not_configured, payment_link_not_allowed, …).
/// </summary>
public class BillingClient
{
    private readonly HttpClient _httpClient;

    public BillingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // ── Checkout (Flujo A, pantalla A4) ───────────────────────────────────────

    /// <summary>
    /// POST /api/billing/checkout-session. The Idempotency-Key header is
    /// mandatory: the caller generates ONE key per checkout attempt and reuses
    /// it on retry so a timeout never creates a second Stripe session (spec A4).
    /// </summary>
    public async Task<CheckoutSessionResource> CreateCheckoutSessionAsync(
        string plan,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, BillingRoutes.CheckoutSession)
        {
            Content = JsonContent.Create(new { plan })
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<CheckoutSessionResource>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("The checkout session response was incomplete.");
    }

    // ── Subscription (A5/A6/A8/A10) ───────────────────────────────────────────

    /// <summary>
    /// GET /api/billing/subscription — the single source of truth for Premium
    /// state. A free account is a normal 200 with status "none" (QA-B6), never
    /// an error; a defensive 404 is also mapped to "none".
    /// </summary>
    public async Task<SubscriptionResource> GetSubscriptionAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BillingRoutes.Subscription, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new SubscriptionResource { Status = SubscriptionStates.None };
        }

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<SubscriptionResource>(cancellationToken: cancellationToken)
               ?? new SubscriptionResource { Status = SubscriptionStates.None };
    }

    /// <summary>
    /// POST /api/billing/subscription/cancel — cancel at period end; access is
    /// kept until currentPeriodEnd (no refunds in MVP).
    /// </summary>
    public async Task<SubscriptionResource> CancelSubscriptionAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(BillingRoutes.SubscriptionCancel, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<SubscriptionResource>(cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("The cancel response was incomplete.");
    }

    // ── Receipts (A6/A12) ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ReceiptResource>> GetReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BillingRoutes.Receipts, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<List<ReceiptResource>>(cancellationToken: cancellationToken)
               ?? new List<ReceiptResource>();
    }

    // ── Trainer payment link (Flujo B) ────────────────────────────────────────

    /// <summary>
    /// PUT /api/trainers/me/payment-link. An empty url removes the published
    /// link and the API answers 204 (QA-B7); a 200 returns the link DTO.
    /// </summary>
    public async Task<PaymentLinkResource?> UpsertPaymentLinkAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            TrainerRoutes.MyPaymentLink, new { url }, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<PaymentLinkResource>(cancellationToken: cancellationToken);
    }

    /// <summary>GET /api/trainers/me/payment-link — null when none published (404).</summary>
    public async Task<PaymentLinkResource?> GetMyPaymentLinkAsync(
        CancellationToken cancellationToken = default)
        => await GetPaymentLinkOrNullAsync(TrainerRoutes.MyPaymentLink, cancellationToken);

    /// <summary>GET /api/trainers/{id}/payment-link — public read; null when the
    /// trainer has not published one (404) (B5).</summary>
    public async Task<PaymentLinkResource?> GetTrainerPaymentLinkAsync(
        string trainerId,
        CancellationToken cancellationToken = default)
        => await GetPaymentLinkOrNullAsync(TrainerRoutes.TrainerPaymentLink(trainerId), cancellationToken);

    private async Task<PaymentLinkResource?> GetPaymentLinkOrNullAsync(
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content
                   .ReadFromJsonAsync<PaymentLinkResource>(cancellationToken: cancellationToken);
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var (code, message) = await ReadProblemAsync(response, cancellationToken);
        throw new BillingApiException(response.StatusCode, code, message);
    }

    private static async Task<(string Code, string Message)> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var fallback = response.StatusCode switch
        {
            HttpStatusCode.Conflict => BillingCodes.SubscriptionActive,
            HttpStatusCode.NotFound => "not_found",
            _ => "billing_error",
        };

        try
        {
            using var problem = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            var root = problem.RootElement;

            var code = root.TryGetProperty("code", out var codeValue)
                ? codeValue.GetString()
                : null;

            var detail = root.TryGetProperty("detail", out var detailValue)
                ? detailValue.GetString()
                : null;
            var title = root.TryGetProperty("title", out var titleValue)
                ? titleValue.GetString()
                : null;

            var message = !string.IsNullOrWhiteSpace(detail)
                ? detail
                : title ?? string.Empty;

            return (string.IsNullOrWhiteSpace(code) ? fallback : code!, message);
        }
        catch (JsonException)
        {
            return (fallback, string.Empty);
        }
    }
}

public static class BillingRoutes
{
    public const string CheckoutSession = "api/billing/checkout-session";
    public const string Subscription = "api/billing/subscription";
    public const string SubscriptionCancel = "api/billing/subscription/cancel";
    public const string Receipts = "api/billing/receipts";
}

public static class TrainerRoutes
{
    public const string MyPaymentLink = "api/trainers/me/payment-link";
    public static string TrainerPaymentLink(string trainerId)
        => $"api/trainers/{trainerId}/payment-link";
}

/// <summary>Stripe-vocabulary status strings used by GET /api/billing/subscription.</summary>
public static class SubscriptionStates
{
    public const string None = "none";
    public const string Incomplete = "incomplete";
    public const string Trialing = "trialing";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Canceled = "canceled";

    public static bool IsLive(string status)
        => status is Trialing or Active or PastDue;
}

/// <summary>Machine-readable ProblemDetails `code` values from the billing API.</summary>
public static class BillingCodes
{
    public const string IdempotencyKeyRequired = "idempotency_key_required";
    public const string PlanRequired = "plan_required";
    public const string PlanNotFound = "plan_not_found";
    public const string IdempotencyConflict = "idempotency_conflict";
    public const string SubscriptionActive = "subscription_active";
    public const string BillingNotConfigured = "billing_not_configured";
    public const string SubscriptionNotFound = "subscription_not_found";
    public const string PaymentLinkNotAllowed = "payment_link_not_allowed";
    public const string PaymentLinkNotFound = "payment_link_not_found";
}

public class BillingApiException : Exception
{
    public BillingApiException(HttpStatusCode statusCode, string code, string message)
        : base(string.IsNullOrWhiteSpace(message) ? code : message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public HttpStatusCode StatusCode { get; }
    public string Code { get; }
}

// ── DTOs (camelCase JSON, mirrors BillingController resources) ───────────────

public class CheckoutSessionResource
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
}

public class SubscriptionResource
{
    public string Status { get; set; } = SubscriptionStates.None;
    public string Plan { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? TrialEnd { get; set; }
    public bool Refunded { get; set; }
}

public class ReceiptResource
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int AmountCents { get; set; }
    public string? ReceiptUrl { get; set; }
}

public class PaymentLinkResource
{
    public string Url { get; set; } = string.Empty;
    public string VerifiedDomain { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
