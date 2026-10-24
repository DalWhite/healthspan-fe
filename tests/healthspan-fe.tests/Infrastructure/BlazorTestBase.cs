using Bunit;
using Bunit.TestDoubles;
using Blazored.LocalStorage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;
using healthspan_fe_wasm_pwa.Shared;

namespace healthspan_fe.tests.Infrastructure;

/// <summary>
/// Base class for bUnit component tests.
/// Registers common services (MudBlazor, MockHttp, TitleService, AppStateService etc.)
/// so individual test classes can focus on the component under test.
/// </summary>
public abstract class BlazorTestBase : TestContext
{
    protected MockHttpMessageHandler MockHttp { get; }
    protected TitleService TitleService { get; }
    protected TestAuthorizationContext AuthContext { get; }

    protected BlazorTestBase()
    {
        // bUnit's default wait ceiling is 1 s. Under full-suite load the render
        // that carries async data can legitimately take longer, which showed up
        // as flaky WaitFor* timeouts (RegisterActivityTests/ProfilePageTests/
        // WorkoutsPageTests) in full-suite runs. Raise the ceiling for every
        // WaitFor* helper that does not pass an explicit timeout.
        DefaultWaitTimeout = TimeSpan.FromSeconds(10);

        MockHttp = new MockHttpMessageHandler();
        TitleService = new TitleService();

        var httpClient = MockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");

        MockHttp.When("/Users/roles")
            .Respond("application/json", "[]");

        // Register via factory so the DI container OWNS the HttpClient and disposes it
        // when TestContext.Dispose() runs. Registering the instance directly
        // (AddSingleton(instance)) leaves it undisposed; a leaked HttpClient that has
        // already served a request can deadlock the next test's async submit chain.
        Services.AddSingleton(_ => httpClient);
        Services.AddSingleton(TitleService);
        Services.AddSingleton(new AppStateService(httpClient));
        Services.AddSingleton(new ThemeState());
        Services.AddSingleton(new ActiveSessionState());
        Services.AddSingleton(new HealthspanApiClient(httpClient));
        Services.AddSingleton(new ActivityClient(httpClient));

        // Offline layer: MainLayout injects these; tests exercise the same
        // code paths with the in-memory store (no JS/SQLite needed).
        var offlineStore = new Healthspan.Offline.InMemoryLocalStore();
        var offlineStatus = new Healthspan.Offline.SyncStatusService();
        var offlineCoordinator = new Healthspan.Offline.SyncCoordinator();
        Services.AddSingleton<Healthspan.Offline.ILocalStore>(offlineStore);
        Services.AddSingleton(offlineStatus);
        Services.AddSingleton(offlineCoordinator);
        Services.AddSingleton(new Healthspan.Offline.LocalMutator(
            offlineStore, offlineStatus));
        Services.AddSingleton(new Healthspan.Offline.SyncEngine(
            () => httpClient, offlineStore, offlineStatus));

        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var syncLocalStorage = Substitute.For<ISyncLocalStorageService>();
        syncLocalStorage.ContainKey(Arg.Any<string>()).Returns(false);
        Services.AddSingleton(syncLocalStorage);
        Services.AddMudServices(MudAppConfig.Configure);

        // Auth
        AuthContext = this.AddTestAuthorization();
        AuthContext.SetAuthorized("test@healthspan.dev");

        // MudBlazor needs JSInterop stubs
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudScrollManager.lockScroll", _ => true);
        JSInterop.SetupVoid("mudScrollManager.unlockScroll", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);
        JSInterop.SetupVoid("mudElementRef.saveFocus", _ => true);
        JSInterop.SetupVoid("mudElementRef.restoreFocus", _ => true);
        JSInterop.SetupVoid("mudScrollSpy.spying", _ => true);
        JSInterop.SetupVoid("mudScrollSpy.unspy", _ => true);
        JSInterop.SetupVoid("mudResizeListener.listenForResize", _ => true);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
