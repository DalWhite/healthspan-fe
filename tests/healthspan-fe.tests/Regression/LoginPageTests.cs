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

        navigationManager.Uri.Should().EndWith("/training/insights");
    }
}
