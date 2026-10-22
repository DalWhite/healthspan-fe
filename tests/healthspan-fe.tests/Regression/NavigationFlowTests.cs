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
    public void RootRedirect_Sends_Authorized_User_To_Training_Insights()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/");

        var cut = RenderComponent<MainLayout>();

        // The redirect happens after the async auth/roles round-trip, so wait
        // for it instead of racing the continuation.
        cut.WaitForAssertion(
            () => navigationManager.Uri.Should().EndWith("/training/insights"),
            TimeSpan.FromSeconds(5));
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
    public void MainLayout_Unauthorized_From_Training_Today_Preserves_ReturnUrl()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        AuthContext.SetNotAuthorized();
        navigationManager.NavigateTo("https://localhost/training/today");

        RenderComponent<MainLayout>();

        navigationManager.Uri.Should().EndWith("/login?returnUrl=%2Ftraining%2Ftoday");
    }

    [Fact]
    public void MainLayout_Unauthorized_Preserves_Query_String_In_ReturnUrl()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        AuthContext.SetNotAuthorized();
        navigationManager.NavigateTo("https://localhost/health/profile?tab=overview");

        RenderComponent<MainLayout>();

        navigationManager.Uri.Should().EndWith("/login?returnUrl=%2Fhealth%2Fprofile%3Ftab%3Doverview");
    }

    [Fact]
    public void MainLayout_Unauthorized_On_Login_Route_Does_Not_Redirect()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        AuthContext.SetNotAuthorized();
        navigationManager.NavigateTo("https://localhost/login");

        RenderComponent<MainLayout>();

        navigationManager.Uri.Should().EndWith("/login");
    }

    [Fact]
    public void MainLayout_And_RedirectToLogin_Agree_On_ReturnUrl()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        AuthContext.SetNotAuthorized();
        navigationManager.NavigateTo("https://localhost/training/today");

        RenderComponent<MainLayout>();
        var mainLayoutTarget = navigationManager.Uri;

        navigationManager.NavigateTo("https://localhost/training/today");
        RenderComponent<RedirectToLogin>();

        navigationManager.Uri.Should().Be(mainLayoutTarget);
    }

    [Fact]
    public void Navigation_Uses_Task_Based_Health_And_Training_Groups()
    {
        var cut = RenderComponent<NavMenu>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Health");
            cut.Markup.Should().Contain("Training");
            // DESIGN.md §4: user-task terminology, no internal concepts.
            cut.Markup.Should().Contain("Hoy");
            cut.Markup.Should().Contain("Historial");
            cut.Markup.Should().Contain("Biblioteca");
            cut.Markup.Should().Contain("Personal Profile");
            cut.Markup.Should().Contain("Insights");
            cut.Markup.Should().Contain("Account &amp; Settings");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/today");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/history");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/library");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "training/insights");
            cut.FindAll("a").Should().Contain(link => link.GetAttribute("href") == "health/profile");
            cut.FindAll("a").Should().NotContain(link => link.GetAttribute("href") == "profile");
        });
    }

    [Fact]
    public void Main_Layout_Renders_The_V1_Bottom_Nav_With_A_Central_Fab()
    {
        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        navigationManager.NavigateTo("https://localhost/training/insights");

        var cut = RenderComponent<MainLayout>();

        cut.WaitForAssertion(() =>
        {
            // DESIGN.md §4: five slots — Hoy · Historial · FAB · Insights · Biblioteca.
            var items = cut.FindAll(".hs-bottom-nav > *");
            items.Should().HaveCount(5);

            var links = cut.FindAll(".hs-bottom-nav a");
            links.Should().HaveCount(4);
            links.Should().Contain(link => link.GetAttribute("href") == "/training/today");
            links.Should().Contain(link => link.GetAttribute("href") == "/training/history");
            links.Should().Contain(link => link.GetAttribute("href") == "/training/insights");
            links.Should().Contain(link => link.GetAttribute("href") == "/training/library");

            var fab = cut.Find(".hs-fab");
            fab.GetAttribute("aria-label").Should().Be("Registrar entreno");

            // Desktop shell carries the same destinations; CSS swaps the two.
            cut.Find(".hs-sidebar .hs-nav-menu");
        });

        // FAB action = quick start from any screen (DESIGN.md §3): it is not a
        // second route to Hoy, it opens the logging state directly (?start=1).
        // Re-query and re-click inside the assertion: the layout re-renders while
        // the auth/roles round-trip completes, and a click on a superseded render
        // frame is dropped — on slower CI machines that race used to fail this.
        cut.WaitForAssertion(() =>
        {
            cut.Find(".hs-fab").Click();
            navigationManager.Uri.Should().EndWith("/training/today?start=1");
        }, TimeSpan.FromSeconds(5));
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

    [Fact]
    public void Profile_Routes_Separate_Training_Insights_From_Health_Settings()
    {
        var trainingRoutes = typeof(
                healthspan_fe_wasm_pwa.Pages.Health.Profile)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template);
        var healthRoutes = typeof(
                healthspan_fe_wasm_pwa.Pages.Health.HealthProfile)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template);

        trainingRoutes.Should().Contain("/profile");
        trainingRoutes.Should().Contain("/training/insights");
        trainingRoutes.Should().NotContain("/health/profile");
        healthRoutes.Should().Contain("/health/profile");
    }
}
