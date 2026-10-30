using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Healthspan.Authentication;
using Healthspan.Authentication.Models;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class RegisterPageTests : BlazorTestBase
{
    private IAccountManagement StubSuccessfulRegistration()
    {
        var account = Substitute.For<IAccountManagement>();
        account.RegisterAsync("new@healthspan.dev", "correct-horse")
            .Returns(new FormResult { Succeeded = true });
        Services.AddSingleton(account);
        return account;
    }

    private IRenderedComponent<Register> RegisterWithReturnUrl(string? returnUrl)
    {
        AuthContext.SetNotAuthorized();
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        if (returnUrl is not null)
        {
            navigationManager.NavigateTo(
                navigationManager.GetUriWithQueryParameter("returnUrl", returnUrl));
        }

        return RenderComponent<Register>();
    }

    [Fact]
    public void Register_Success_From_Deep_Link_Offers_The_Original_Destination()
    {
        // D3 (unauth-redirect-state.md): registering from a shared link must
        // pay the user back with the destination they came for.
        StubSuccessfulRegistration();
        var cut = RegisterWithReturnUrl("/training/history");

        var inputs = cut.FindAll("input");
        inputs[0].Change("new@healthspan.dev");
        inputs[1].Change("correct-horse");
        inputs[2].Change("correct-horse");

        cut.Find("button:contains('Register')").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain(
                "Cuenta creada. Inicia sesión para volver a tu historial de entrenamientos.");
        });
    }

    [Fact]
    public void Register_Success_Cta_Returns_To_Login_With_ReturnUrl()
    {
        StubSuccessfulRegistration();
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RegisterWithReturnUrl("/training/history");

        var inputs = cut.FindAll("input");
        inputs[0].Change("new@healthspan.dev");
        inputs[1].Change("correct-horse");
        inputs[2].Change("correct-horse");

        cut.Find("button:contains('Register')").Click();

        cut.WaitForAssertion(() =>
            cut.Find("button:contains('Iniciar sesión y volver')").Should().NotBeNull());

        // ≤2 taps back to the original destination.
        cut.Find("button:contains('Iniciar sesión y volver')").Click();

        navigationManager.Uri.Should()
            .EndWith("/login?returnUrl=%2Ftraining%2Fhistory");
    }

    [Fact]
    public void Register_Rejects_Unsafe_ReturnUrl_In_Success_Panel()
    {
        StubSuccessfulRegistration();
        var cut = RegisterWithReturnUrl("//malicious.example");

        var inputs = cut.FindAll("input");
        inputs[0].Change("new@healthspan.dev");
        inputs[1].Change("correct-horse");
        inputs[2].Change("correct-horse");

        cut.Find("button:contains('Register')").Click();

        cut.WaitForAssertion(() =>
        {
            // Same validation contract as login: no contextual CTA for an
            // external returnUrl — the generic success copy is shown instead.
            cut.Markup.Should().NotContain("Iniciar sesión y volver");
            cut.Markup.Should().Contain("You successfully registered.");
        });
    }

    [Fact]
    public void Register_Without_ReturnUrl_Keeps_The_Generic_Success_Copy()
    {
        StubSuccessfulRegistration();
        var cut = RegisterWithReturnUrl(null);

        var inputs = cut.FindAll("input");
        inputs[0].Change("new@healthspan.dev");
        inputs[1].Change("correct-horse");
        inputs[2].Change("correct-horse");

        cut.Find("button:contains('Register')").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("You successfully registered.");
            cut.Markup.Should().NotContain("Vista protegida");
        });
    }
}
