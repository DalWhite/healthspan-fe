using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components.Billing;
using healthspan_fe_wasm_pwa.Pages.Billing;
using healthspan_fe_wasm_pwa.Pages.Health;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Flujo de pago Fase 1 (pago-ux-spec-t_5b46e650 + billing-api-t_c0e8a770):
/// copy canónico, CTA único con trial, idempotency-key, handoff a Stripe,
/// gestión de suscripción honesta (A7/A8) y payment link del entrenador (B1-B5).
/// </summary>
public class PaymentFlowTests : BlazorTestBase
{
    private const string SubscriptionJson = """
        {
          "status": "active",
          "plan": "premium-monthly",
          "currentPeriodEnd": "2026-11-24T00:00:00Z",
          "cancelAtPeriodEnd": false,
          "trialEnd": null,
          "refunded": false
        }
        """;

    private const string TrialingJson = """
        {
          "status": "trialing",
          "plan": "premium-monthly",
          "currentPeriodEnd": "2026-10-31T00:00:00Z",
          "cancelAtPeriodEnd": false,
          "trialEnd": "2026-10-31T00:00:00Z",
          "refunded": false
        }
        """;

    private const string CancelScheduledJson = """
        {
          "status": "active",
          "plan": "premium-monthly",
          "currentPeriodEnd": "2026-11-24T00:00:00Z",
          "cancelAtPeriodEnd": true,
          "trialEnd": null,
          "refunded": false
        }
        """;

    private const string NoneJson = """{ "status": "none" }""";

    private BillingClient BillingApi()
    {
        var httpClient = MockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");
        return new BillingClient(httpClient);
    }

    private void MockSubscription(string json) =>
        MockHttp.When(HttpMethod.Get, "/api/billing/subscription")
            .Respond("application/json", json);

    private void MockReceipts(string json) =>
        MockHttp.When(HttpMethod.Get, "/api/billing/receipts")
            .Respond("application/json", json);

    // ── BillingClient (contrato API) ───────────────────────────────────────

    [Fact]
    public async Task Checkout_PostsIdempotencyKey_And_ReturnsCheckoutUrl()
    {
        string? idempotencyKey = null;
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond(req =>
            {
                req.Headers.TryGetValues("Idempotency-Key", out var values);
                idempotencyKey = values?.FirstOrDefault();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{ "sessionId": "cs_1", "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test", "plan": "premium-monthly" }""",
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            });

        var session = await BillingApi().CreateCheckoutSessionAsync(
            "premium-monthly", "key-123");

        session.CheckoutUrl.Should().Be("https://checkout.stripe.com/c/pay/cs_test");
        idempotencyKey.Should().Be("key-123");
    }

    [Fact]
    public async Task Checkout_409_SurfacesSubscriptionActiveCode()
    {
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond(HttpStatusCode.Conflict, new StringContent(
                """{ "type": "https://healthspan.local/errors/subscription-active", "title": "subscription_active", "status": 409, "detail": "x", "code": "subscription_active" }""",
                System.Text.Encoding.UTF8, "application/json"));

        var act = () => BillingApi().CreateCheckoutSessionAsync(
            "premium-monthly", "key-1");
        var ex = await act.Should().ThrowAsync<BillingApiException>();
        ex.Which.Code.Should().Be(BillingCodes.SubscriptionActive);
    }

    [Fact]
    public async Task Checkout_503_SurfacesBillingNotConfiguredCode()
    {
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond(HttpStatusCode.ServiceUnavailable, new StringContent(
                """{ "type": "https://healthspan.local/errors/billing-not-configured", "title": "billing_not_configured", "status": 503, "detail": "x", "code": "billing_not_configured" }""",
                System.Text.Encoding.UTF8, "application/json"));

        var act = () => BillingApi().CreateCheckoutSessionAsync(
            "premium-monthly", "key-1");
        var ex = await act.Should().ThrowAsync<BillingApiException>();
        ex.Which.Code.Should().Be(BillingCodes.BillingNotConfigured);
    }

    [Fact]
    public async Task Subscription_404_MapsToNoneStatus()
    {
        MockHttp.When(HttpMethod.Get, "/api/billing/subscription")
            .Respond(HttpStatusCode.NotFound);

        var subscription = await BillingApi().GetSubscriptionAsync();

        subscription.Status.Should().Be(SubscriptionStates.None);
    }

    [Fact]
    public async Task TrainerPaymentLink_404_MapsToNull()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/t1/payment-link")
            .Respond(HttpStatusCode.NotFound);

        var link = await BillingApi().GetTrainerPaymentLinkAsync("t1");

        link.Should().BeNull();
    }

    [Fact]
    public async Task RemovePaymentLink_PutsEmptyUrl_And_Maps204ToNull()
    {
        string? body = null;
        MockHttp.When(HttpMethod.Put, "/api/trainers/me/payment-link")
            .Respond(req =>
            {
                body = req.Content?.ReadAsStringAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

        var link = await BillingApi().UpsertPaymentLinkAsync(string.Empty);

        link.Should().BeNull();
        // JsonContent.Create serializa compacto: {"url":""}.
        body.Should().Be("""{"url":""}""");
    }

    // ── A2/A4 — Premium: selección de plan y handoff ───────────────────────

    [Fact]
    public void Premium_RendersCanonicalTrialCopy_And_TwoPlans()
    {
        MockSubscription(NoneJson);
        var cut = RenderComponent<Premium>();

        cut.WaitForState(() => cut.Markup.Contains("Mensual"));

        // Copy canónico exacto (QA-B9) + ancla de confianza.
        cut.Markup.Should().Contain(
            "7 días gratis, luego se cobra automáticamente. Cancela cuando quieras.");
        cut.Markup.Should().Contain(
            "Tu registro y todo tu historial siguen siendo gratis para siempre.");
        // CTA único (QA-B9): no existe opción "pago directo sin trial".
        cut.Markup.Should().Contain("Empezar prueba gratis de 7 días");
        // Divulgación de pasarela (A4).
        cut.Markup.Should().Contain("Stripe es la pasarela que procesa el pago");
        // Precios con los dos planes del catálogo.
        cut.Markup.Should().Contain("9,99 €");
        cut.Markup.Should().Contain("89,99 €");
        cut.Markup.Should().Contain("2 meses gratis");
    }

    [Fact]
    public void Premium_MonthlyPlanIsPreselected()
    {
        MockSubscription(NoneJson);
        var cut = RenderComponent<Premium>();

        cut.WaitForState(() => cut.Markup.Contains("Mensual"));

        var cards = cut.FindAll(".hs-plan-card");
        cards.Should().HaveCount(2);
        // D3: mensual preseleccionado.
        cards[0].GetAttribute("aria-checked").Should().Be("true");
        cards[1].GetAttribute("aria-checked").Should().Be("false");
    }

    [Fact]
    public void Premium_ExistingSubscription_HidesPlanSelection()
    {
        MockSubscription(SubscriptionJson);
        var cut = RenderComponent<Premium>();

        cut.WaitForState(() => cut.Markup.Contains("Ya tienes una suscripción Premium"));

        cut.Markup.Should().NotContain("Empezar prueba gratis de 7 días");
        cut.FindAll(".hs-checkout-cta").Should().BeEmpty();
        cut.Markup.Should().Contain("Gestionar suscripción");
    }

    [Fact]
    public void Premium_CheckoutNotConfigured_ShowsHonestError_AndKeepsRetry()
    {
        MockSubscription(NoneJson);
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond(HttpStatusCode.ServiceUnavailable, new StringContent(
                """{ "type": "https://healthspan.local/errors/billing-not-configured", "title": "billing_not_configured", "status": 503, "detail": "x", "code": "billing_not_configured" }""",
                System.Text.Encoding.UTF8, "application/json"));

        var cut = RenderComponent<Premium>();
        cut.WaitForState(() => cut.Markup.Contains("Mensual"));

        cut.Find(".hs-checkout-cta").Click();

        cut.WaitForState(() => cut.Markup.Contains("El pago no está disponible"));
        cut.Find(".hs-reg-banner").GetAttribute("role").Should().Be("alert");
        // A9: el botón queda disponible para reintentar.
        cut.Find(".hs-checkout-cta").GetAttribute("disabled").Should().BeNull();
    }

    [Fact]
    public void Premium_CheckoutConflict_ReportsSubscriptionActive()
    {
        MockSubscription(NoneJson);
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond(HttpStatusCode.Conflict, new StringContent(
                """{ "type": "https://healthspan.local/errors/subscription-active", "title": "subscription_active", "status": 409, "detail": "x", "code": "subscription_active" }""",
                System.Text.Encoding.UTF8, "application/json"));

        var cut = RenderComponent<Premium>();
        cut.WaitForState(() => cut.Markup.Contains("Mensual"));

        cut.Find(".hs-checkout-cta").Click();

        cut.WaitForState(() => cut.Markup.Contains("Ya tienes una suscripción Premium activa."));
    }

    [Fact]
    public void Premium_CheckoutSuccess_NavigatesToStripeCheckout()
    {
        MockSubscription(NoneJson);
        MockHttp.When(HttpMethod.Post, "/api/billing/checkout-session")
            .Respond("application/json",
                """{ "sessionId": "cs_1", "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_123", "plan": "premium-monthly" }""");

        var cut = RenderComponent<Premium>();
        cut.WaitForState(() => cut.Markup.Contains("Mensual"));

        cut.Find(".hs-checkout-cta").Click();

        // A4: handoff hosted con forceLoad (salida real de la SPA). El POST es
        // asíncrono: esperar a que la navegación quede registrada.
        var navigation = (FakeNavigationManager)Services.GetRequiredService<NavigationManager>();
        cut.WaitForState(() => navigation.History.Count > 0, TimeSpan.FromSeconds(10));
        navigation.History.Should().Contain(n =>
            n.Uri == "https://checkout.stripe.com/c/pay/cs_test_123" && n.Options.ForceLoad);
    }

    // ── A6/A8 — BillingHome: gestión honesta ───────────────────────────────

    [Fact]
    public void BillingHome_NoSubscription_ShowsEmptyState()
    {
        MockSubscription(NoneJson);
        MockReceipts("[]");

        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("Sin suscripción Premium"));

        // D1: la nota repite que lo básico es gratis siempre.
        cut.Markup.Should().Contain("gratis para siempre");
        cut.Markup.Should().Contain("Ver planes Premium");
    }

    [Fact]
    public void BillingHome_Trialing_ShowsTrialBannerWithAbsoluteDate()
    {
        MockSubscription(TrialingJson);
        MockReceipts("[]");

        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("Prueba Premium · termina el"));
        // A12: el vacío útil aparece cuando los recibos terminan de cargar.
        cut.WaitForState(() => cut.Markup.Contains("Aún no hay recibos"), TimeSpan.FromSeconds(10));

        // D5: fecha absoluta + "Día X de 7".
        cut.Markup.Should().Contain("31 oct 2026");
        cut.Markup.Should().Contain("de 7");
        // A12: vacío útil que explica el trial.
        cut.Markup.Should().Contain("Aún no hay recibos");
        cut.Markup.Should().Contain("Durante la prueba de 7 días");
    }

    [Fact]
    public void BillingHome_Active_ShowsStatusBadgeWithRenewalDate()
    {
        MockSubscription(SubscriptionJson);
        MockReceipts("""
            [{ "id": "HS-0001", "date": "2026-10-24T10:00:00Z",
               "amountCents": 999, "receiptUrl": "https://invoice.stripe.com/i/in_123" }]
            """);

        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("Premium activo · renueva el"));
        // Los recibos se cargan en paralelo: esperar al recibo mockeado.
        cut.WaitForState(() => cut.Markup.Contains("HS-0001"), TimeSpan.FromSeconds(10));

        // D9: icono + texto (no solo color) + fecha absoluta.
        cut.FindAll(".hs-status-badge .mud-icon-root").Should().NotBeEmpty();
        cut.Markup.Should().Contain("24 nov 2026");
        // Recibo con enlace externo.
        cut.Markup.Should().Contain("HS-0001");
        var receiptLink = cut.Find("a.hs-receipt-link");
        receiptLink.GetAttribute("target").Should().Be("_blank");
        receiptLink.GetAttribute("rel").Should().Contain("noopener");
    }

    [Fact]
    public void BillingHome_CancelAtPeriodEnd_ShowsHonestCancelledState()
    {
        MockSubscription(CancelScheduledJson);
        MockReceipts("[]");

        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("Cancelado · acceso hasta"));

        // A8 exacto: badge + "no se hará ningún cobro después" + Reactivar.
        cut.Markup.Should().Contain("Cancelado · acceso hasta 24 nov 2026");
        cut.Markup.Should().Contain("No se hará ningún cobro después");
        cut.Find("a.hs-billing-reactivate").Should().NotBeNull();
    }

    [Fact]
    public void BillingHome_LoadFailure_ShowsRetryState()
    {
        MockHttp.When(HttpMethod.Get, "/api/billing/subscription")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("No se pudo cargar tu suscripción"));

        cut.Markup.Should().Contain("Reintentar");
    }

    [Fact]
    public void BillingHome_CancelFlow_SheetConfirmsAndUpdatesState()
    {
        MockSubscription(SubscriptionJson);
        MockReceipts("[]");
        MockHttp.When(HttpMethod.Post, "/api/billing/subscription/cancel")
            .Respond("application/json", CancelScheduledJson);

        var dialogs = RenderComponent<MudDialogProvider>();
        var cut = RenderComponent<BillingHome>();
        cut.WaitForState(() => cut.Markup.Contains("Premium activo · renueva el"));
        cut.WaitForState(() => cut.Markup.Contains("hs-billing-receipts") && !cut.Markup.Contains("hs-skeleton"), TimeSpan.FromSeconds(10));

        // A7: abrir sheet desde el switch (MudSwitch dispara onchange).
        cut.Find(".mud-switch-input").Change(true);
        dialogs.WaitForState(() => dialogs.FindAll(".mud-dialog").Count > 0);

        // Hoja honesta (D10): qué se bloquea y qué se conserva, con fecha.
        // Copy canónico; aserciones insensibles a los saltos de línea del fuente.
        Normalize(dialogs.Markup).Should().Contain("Mantendrás todo el acceso hasta el");
        Normalize(dialogs.Markup).Should().Contain("se bloquea el análisis avanzado");
        Normalize(dialogs.Markup).Should().Contain("no borramos nada");

        dialogs.FindAll("button")
            .First(b => b.TextContent.Contains("Confirmar cancelación"))
            .Click();

        cut.WaitForState(() => cut.Markup.Contains("Cancelado · acceso hasta"));
        cut.Markup.Should().Contain("No se hará ningún cobro después");
    }

    // ── A10/A5/A9 — vuelta de Stripe: sondeo, nunca inferencia ─────────────

    [Fact]
    public void CheckoutSuccess_ConfirmsWhenSubscriptionBecomesActive()
    {
        var pollCalls = 0;
        MockHttp.When(HttpMethod.Get, "/api/billing/subscription")
            .Respond(_ =>
            {
                pollCalls++;
                var json = pollCalls <= 2 ? NoneJson : SubscriptionJson;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        var cut = RenderComponent<CheckoutSuccess>(p => p
            .Add(c => c.PollIntervalSeconds, 0)
            .Add(c => c.MaxWaitSeconds, 30));

        cut.WaitForState(() => cut.Markup.Contains("Pago confirmado"),
            TimeSpan.FromSeconds(15));

        // A5: lista de lo desbloqueado + fecha de renovación.
        cut.Markup.Should().Contain("Análisis avanzado de entrenamiento (Insights) desbloqueado");
        cut.Markup.Should().Contain("Renovación el");
    }

    [Fact]
    public void CheckoutSuccess_Timeout_ShowsHonestFailureCopy()
    {
        MockSubscription(NoneJson);

        var cut = RenderComponent<CheckoutSuccess>(p => p
            .Add(c => c.PollIntervalSeconds, 0)
            .Add(c => c.MaxWaitSeconds, 0));

        cut.WaitForState(() => cut.Markup.Contains("El pago no se completó"),
            TimeSpan.FromSeconds(15));
        var copy = cut.Markup;

        // A9: copy honesto, sin alarmismo (el markup conserva los saltos de línea
        // del fuente Razor → aserción insensible a whitespace).
        Normalize(copy).Should().Contain("No se te ha cobrado nada");
        copy.Should().Contain("Reintentar");
    }

    [Fact]
    public void CheckoutSuccess_PollingRegionIsAriaLivePolite()
    {
        MockSubscription(SubscriptionJson);

        var cut = RenderComponent<CheckoutSuccess>(p => p
            .Add(c => c.PollIntervalSeconds, 0)
            .Add(c => c.MaxWaitSeconds, 30));

        cut.WaitForState(() => cut.Markup.Contains("Pago confirmado"),
            TimeSpan.FromSeconds(15));

        cut.Find("div[aria-live=\"polite\"]").Should().NotBeNull();
    }

    [Fact]
    public void CheckoutCancelled_ShowsNoChargeCopy_And_TrialIntactNote()
    {
        MockSubscription(TrialingJson);

        var cut = RenderComponent<CheckoutCancelled>();
        cut.WaitForState(() => cut.Markup.Contains("Pago cancelado"));

        cut.Markup.Should().Contain("No se te ha cobrado nada.");
        cut.Markup.Should().Contain("Tu prueba de 7 días sigue activa.");
        cut.Markup.Should().Contain("Reintentar pago");
    }

    // ── Flujo B — payment link del entrenador ──────────────────────────────

    [Fact]
    public void TrainerPaymentLink_Published_ShowsCardWithExternalDisclosure()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/t1/payment-link")
            .Respond("application/json", """
                { "url": "https://buy.stripe.com/test_123",
                  "verifiedDomain": "buy.stripe.com",
                  "updatedAt": "2026-10-20T10:00:00Z" }
                """);

        var cut = RenderComponent<TrainerPaymentLink>(p => p.Add(c => c.TrainerId, "t1"));
        cut.WaitForState(() => cut.Markup.Contains("buy.stripe.com"));

        var link = cut.Find("a.hs-payment-link-open");
        link.GetAttribute("target").Should().Be("_blank");
        link.GetAttribute("rel").Should().Contain("noopener");
        // D7: divulgación de sitio externo accesible (whitespace-insensitive).
        link.QuerySelector(".hs-visually-hidden")!
            .TextContent.Trim().Trim('(', ')').Should().Be("abre un sitio externo");
        cut.Markup.Should().Contain("Healthspan no cobra, no retiene");
    }

    [Fact]
    public void TrainerPaymentLink_NoLink_ShowsNeutralNote()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/t1/payment-link")
            .Respond(HttpStatusCode.NotFound);

        var cut = RenderComponent<TrainerPaymentLink>(p => p.Add(c => c.TrainerId, "t1"));
        cut.WaitForState(() => cut.Markup.Contains("no tiene enlace de pago"));

        cut.Markup.Should().Contain("Acuerda el pago directamente con él.");
        cut.FindAll(".hs-payment-link-open").Should().BeEmpty();
    }

    [Fact]
    public void PaymentLinkForm_AllowlistIsVisibleBeforeInput()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/me/payment-link")
            .Respond(HttpStatusCode.NotFound);

        var cut = RenderComponent<PaymentLinkForm>();
        cut.WaitForElement("#payment-link-url");

        // D8: allowlist ANTES del <input> (el id también aparece en el label
        // for=, así que se compara contra el elemento input real).
        var markup = cut.Markup;
        markup.IndexOf("Direcciones permitidas", StringComparison.Ordinal)
            .Should().BeLessThan(markup.IndexOf("<input id=\"payment-link-url\"", StringComparison.Ordinal));
        markup.Should().Contain("buy.stripe.com");
        markup.Should().Contain("paypal.me");
    }

    [Fact]
    public void PaymentLinkForm_InvalidDomain_KeepsValueAndShowsExample()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/me/payment-link")
            .Respond(HttpStatusCode.NotFound);
        MockHttp.When(HttpMethod.Put, "/api/trainers/me/payment-link")
            .Respond(HttpStatusCode.BadRequest, new StringContent(
                """{ "type": "https://healthspan.local/errors/payment-link-not-allowed", "title": "payment_link_not_allowed", "status": 400, "detail": "x", "code": "payment_link_not_allowed" }""",
                System.Text.Encoding.UTF8, "application/json"));

        var cut = RenderComponent<PaymentLinkForm>();
        cut.WaitForElement("#payment-link-url");

        cut.Find("#payment-link-url").Input("https://paypal.me/entrenador-fuera-de-allowlist");
        cut.FindAll("button").First(b => b.TextContent.Contains("Publicar enlace")).Click();

        cut.WaitForState(() => cut.Markup.Contains("no está en una dirección permitida"));
        // B2: valor conservado + ejemplo válido + role=alert.
        cut.Find("#payment-link-url").GetAttribute("value")
            .Should().Be("https://paypal.me/entrenador-fuera-de-allowlist");
        cut.Find(".hs-payment-link-error").GetAttribute("role").Should().Be("alert");
        cut.Markup.Should().Contain("https://buy.stripe.com/…");
    }

    [Fact]
    public void PaymentLinkForm_SaveSuccess_ShowsPublishedBadge()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/me/payment-link")
            .Respond(HttpStatusCode.NotFound);
        MockHttp.When(HttpMethod.Put, "/api/trainers/me/payment-link")
            .Respond("application/json", """
                { "url": "https://buy.stripe.com/test_abc",
                  "verifiedDomain": "buy.stripe.com",
                  "updatedAt": "2026-10-24T10:00:00Z" }
                """);

        var cut = RenderComponent<PaymentLinkForm>();
        cut.WaitForElement("#payment-link-url");

        cut.Find("#payment-link-url").Input("https://buy.stripe.com/test_abc");
        cut.FindAll("button").First(b => b.TextContent.Contains("Publicar enlace")).Click();

        cut.WaitForState(() => cut.Markup.Contains("Publicado · visible para tus trainees"));
        // B3: una vez publicado aparece la acción de quitar.
        cut.FindAll("button").Should().Contain(b => b.TextContent.Contains("Quitar enlace"));
    }

    [Fact]
    public void PaymentLinkForm_Remove_ClearsPublishedState()
    {
        MockHttp.When(HttpMethod.Get, "/api/trainers/me/payment-link")
            .Respond("application/json", """
                { "url": "https://buy.stripe.com/test_abc",
                  "verifiedDomain": "buy.stripe.com",
                  "updatedAt": "2026-10-24T10:00:00Z" }
                """);
        MockHttp.When(HttpMethod.Put, "/api/trainers/me/payment-link")
            .Respond(HttpStatusCode.NoContent);

        var cut = RenderComponent<PaymentLinkForm>();
        cut.WaitForState(() => cut.Markup.Contains("Publicado · visible para tus trainees"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Quitar enlace")).Click();

        cut.WaitForState(() => !cut.Markup.Contains("Publicado · visible para tus trainees"));
    }

    // ── A1 — paywall solo en análisis avanzado (D1) ────────────────────────

    private void MockInsightsData()
    {
        MockHttp.When("/Workouts/weekly-summary?weeks=12")
            .Respond("application/json", """{ "weeks": [] }""");
        MockHttp.When("/HealthProfile")
            .Respond("application/json", """{ "biologicalSex": 0 }""");
    }

    [Fact]
    public void Insights_NoSubscription_ShowsUpgradeCard_ButNotAHardLock()
    {
        MockInsightsData();
        MockSubscription(NoneJson);

        var cut = RenderComponent<Profile>();
        // Two chained fetches (insights + subscription) before the card lands: the 1s
        // default races on slower CI runners.
        cut.WaitForState(
            () => cut.Markup.Contains("Análisis avanzado con Premium")
                  && cut.Markup.Contains("No training data yet"),
            TimeSpan.FromSeconds(10));

        // La card es un aviso con CTA; el contenido de la página sigue visible (D1).
        cut.Markup.Should().Contain("gratis para siempre");
        cut.Find("a.hs-paywall-cta").GetAttribute("href").Should().Be("premium");
        cut.Markup.Should().Contain("No training data yet");
    }

    [Fact]
    public void Insights_ActiveSubscription_NoUpgradeCard()
    {
        MockInsightsData();
        MockSubscription(SubscriptionJson);

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => cut.Markup.Contains("No training data yet"));

        cut.Markup.Should().NotContain("Análisis avanzado con Premium");
    }

    [Fact]
    public void Insights_SubscriptionLoadFailure_DoesNotBlockAnalysis()
    {
        MockInsightsData();
        MockHttp.When(HttpMethod.Get, "/api/billing/subscription")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<Profile>();
        cut.WaitForState(() => cut.Markup.Contains("No training data yet"));

        // Sin confirmación no se interrumpe el análisis (matriz §4).
        cut.Markup.Should().NotContain("Análisis avanzado con Premium");
    }

    // ── Navegación ─────────────────────────────────────────────────────────

    [Fact]
    public void NavMenu_ExposesPremiumEntry()
    {
        var cut = RenderComponent<healthspan_fe_wasm_pwa.Shared.NavMenu>();

        cut.WaitForState(() => cut.Markup.Contains("Premium"));
        cut.FindAll("a").Should().Contain(a => a.GetAttribute("href") == "billing");
    }

    // Copy canónico en varias aserciones: el render conserva los saltos de línea
    // del fuente Razor, así que se compara en una sola línea.
    private static string Normalize(string markup)
        => Regex.Replace(markup, @"\s+", " ").Trim();
}
