using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Pages;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using healthspan_fe_wasm_pwa.Pages.ExerciseTypes;
using healthspan_fe_wasm_pwa.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class NavigationFlowTests : BlazorTestBase
{
    [Fact]
    public void RootRedirect_Sends_Authorized_User_To_Dashboard()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/");

        RenderComponent<MainLayout>();

        navigationManager.Uri.Should().EndWith("/health/dashboard");
    }

    [Fact]
    public void RootRedirect_Sends_Unauthorized_User_To_Login()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        AuthContext.SetNotAuthorized();
        navigationManager.NavigateTo("https://localhost/");

        RenderComponent<MainLayout>();

        navigationManager.Uri.Should().EndWith("/login");
    }

    [Fact]
    public void RedirectToLogin_Preserves_ReturnUrl()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/health/profile?tab=overview");

        RenderComponent<RedirectToLogin>();

        navigationManager.Uri.Should().EndWith("/login?returnUrl=%2Fhealth%2Fprofile%3Ftab%3Doverview");
    }

    [Fact]
    public void Navigation_Uses_Task_Based_Health_And_Training_Groups()
    {
        var cut = RenderComponent<NavMenu>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Health");
            cut.Markup.Should().Contain("Training");
            cut.Markup.Should().Contain("Today");
            cut.Markup.Should().Contain("Training History");
            cut.Markup.Should().Contain("Exercise Library");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/today");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/history");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/library");
        });
    }

    [Fact]
    public void Main_Layout_Provides_Left_Aligned_Menu_Launcher_In_Mobile_Navigation()
    {
        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll(".hs-mobile-bottom-nav a");
            links.Should().HaveCount(4);
            links.Should().Contain(link => link.GetAttribute("href") == "/training/today");
            links.Should().Contain(link => link.GetAttribute("href") == "/training/history");
            links.Should().Contain(link => link.GetAttribute("href") == "/training/library");

            var items = cut.FindAll(".hs-mobile-bottom-nav > *");
            items.Should().HaveCount(5);
            items[0].ClassList.Should().Contain("hs-mobile-nav-menu");
            items[0].TextContent.Should().Contain("Menu");
            items[0].GetAttribute("aria-controls").Should().Be("app-navigation-drawer");
        });

        var initialExpandedState = cut.Find(".hs-mobile-nav-menu").GetAttribute("aria-expanded");
        cut.Find(".hs-mobile-nav-menu").Click();
        cut.Find(".hs-mobile-nav-menu").GetAttribute("aria-expanded").Should().NotBe(initialExpandedState);
    }

    [Theory]
    [InlineData(typeof(Workouts), "/workouts", "/training/history")]
    [InlineData(typeof(ExerciseTypes), "/exercises/types", "/training/library")]
    public void Training_Pages_Preserve_Legacy_Routes_Alongside_New_Routes(
        Type componentType,
        string legacyRoute,
        string newRoute)
    {
        var routes = componentType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToList();

        routes.Should().Contain(legacyRoute);
        routes.Should().Contain(newRoute);
    }

    [Fact]
    public void Today_Uses_A_Dedicated_Session_Page_And_Preserves_Legacy_Create()
    {
        var legacyRoutes = typeof(CreateExercises)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template);
        var todayRoutes = typeof(TodayWorkout)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template);

        legacyRoutes.Should().Contain("/exercises/create/{TraineeId?}");
        todayRoutes.Should().Contain("/training/today");
    }
}
