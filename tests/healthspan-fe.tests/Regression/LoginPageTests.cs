using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Healthspan.Authentication;
using Healthspan.Authentication.Models;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components.Identity;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class LoginPageTests : BlazorTestBase
{
    [Fact]
    public void Login_Renders_A_Single_Visible_Page_Heading()
    {
        var account = Substitute.For<IAccountManagement>();
        Services.AddSingleton(account);

        var cut = RenderComponent<Login>();

        var headings = cut.FindAll("h1");
        headings.Should().ContainSingle();
        headings[0].TextContent.Should().Be("Sign in");
    }

    [Fact]
    public async Task Google_Login_Navigates_To_Requested_Local_ReturnUrl()
    {
        var account = Substitute.For<IAccountManagement>();
        account.LoginWithGoogleAsync("google-credential")
            .Returns(new FormResult { Succeeded = true });
        Services.AddSingleton(account);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            navigationManager.GetUriWithQueryParameter("returnUrl", "/training/history"));
        var cut = RenderComponent<Login>();

        await cut.InvokeAsync(() => cut.Instance.HandleGoogleCredentialAsync("google-credential"));

        navigationManager.Uri.Should().EndWith("/training/history");
    }

    [Theory]
    [InlineData("https://malicious.example")]
    [InlineData("//malicious.example")]
    public async Task Google_Login_Rejects_NonLocal_ReturnUrl(string returnUrl)
    {
        var account = Substitute.For<IAccountManagement>();
        account.LoginWithGoogleAsync("google-credential")
            .Returns(new FormResult { Succeeded = true });
        Services.AddSingleton(account);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            navigationManager.GetUriWithQueryParameter("returnUrl", returnUrl));
        var cut = RenderComponent<Login>();

        await cut.InvokeAsync(() => cut.Instance.HandleGoogleCredentialAsync("google-credential"));

        navigationManager.Uri.Should().EndWith("/training/today");
    }

    [Fact]
    public void Invalid_Email_Shows_The_Error_On_The_Field_And_Keeps_What_The_User_Typed()
    {
        var account = Substitute.For<IAccountManagement>();
        Services.AddSingleton(account);

        var cut = RenderComponent<Login>();
        cut.FindAll("input")[0].Change("not-an-email");

        cut.WaitForAssertion(() =>
        {
            // NN/g: the message sits next to the field, names the fix, and the
            // typed value is never thrown away.
            cut.Markup.Should().Contain("Use an email address like name@example.com.");
            cut.FindComponents<MudTextField<string>>()[0].Instance.Value
                .Should().Be("not-an-email");
            // A field problem must not escalate into a generic form-wide banner.
            cut.FindAll(".hs-login-formerror").Should().BeEmpty();
            account.DidNotReceive().LoginAsync(Arg.Any<string>(), Arg.Any<string>());
        });
    }

    [Theory]
    [InlineData("Invalid login attempt.",
        "We couldn't match that email and password. Check them and try again.")]
    [InlineData("This account is temporarily locked.",
        "Too many attempts. Wait a few minutes and try again.")]
    public void Failed_Sign_In_Restates_The_Server_Error_In_Plain_Language(
        string serverError, string expectedMessage)
    {
        var account = Substitute.For<IAccountManagement>();
        account.LoginAsync("wall@healthspan.dev", "wrong-password")
            .Returns(new FormResult { Succeeded = false, ErrorList = [serverError] });
        Services.AddSingleton(account);

        var cut = RenderComponent<Login>();
        var inputs = cut.FindAll("input");
        inputs[0].Change("wall@healthspan.dev");
        inputs[1].Change("wrong-password");

        // Through the button, the way a user does it, so the failure lands in
        // the rendered form and not just in the service call.
        cut.Find("button:contains('Log In')").Click();

        cut.WaitForAssertion(() =>
        {
            var banner = cut.Find(".hs-login-formerror");
            banner.TextContent.Should().Contain(expectedMessage);
            banner.GetAttribute("role").Should().Be("alert");
            cut.FindComponents<MudTextField<string>>()[0].Instance.Value
                .Should().Be("wall@healthspan.dev",
                    "a failed sign-in never erases what the user typed");
        });
    }

    [Fact]
    public async Task Email_Login_Lands_Back_In_Origin_Context()
    {
        var account = Substitute.For<IAccountManagement>();
        account.LoginAsync("test@healthspan.dev", "correct-horse")
            .Returns(new FormResult { Succeeded = true });
        Services.AddSingleton(account);

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo(
            navigationManager.GetUriWithQueryParameter("returnUrl", "/training/today"));
        var cut = RenderComponent<Login>();

        var inputs = cut.FindAll("input");
        inputs[0].Change("test@healthspan.dev");
        inputs[1].Change("correct-horse");

        await cut.InvokeAsync(() => cut.Instance.DoLoginAsync());

        navigationManager.Uri.Should().EndWith("/training/today");
    }
}
