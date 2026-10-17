using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using healthspan_fe_wasm_pwa;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Healthspan.Authentication;
using Healthspan.Offline;
using Blazored.LocalStorage;
using healthspan_fe_wasm_pwa.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
// Re-register every Blazored service as singleton (Blazored registers them
// scoped; our app-wide singletons cannot consume scoped services). Their
// concrete types are internal, so we rewrite their own descriptors.
foreach (var d in builder.Services.Where(d =>
    d.ServiceType.FullName!.StartsWith("Blazored.LocalStorage")).ToList())
{
    builder.Services.Remove(d);
    builder.Services.Add(new ServiceDescriptor(
        d.ServiceType,
        d.ImplementationType ?? d.ServiceType,
        ServiceLifetime.Singleton));
}
builder.Services.AddAuthorizationCore();

// WASM has a single application scope; these services hold per-app state
// (JS interop, localStorage, HTTP handler chain). Registering them as
// singletons keeps the auth provider, its HttpClient/offline chain, and the
// CascadingAuthenticationState guard on ONE shared instance. With scoped
// registrations DI can hand out two auth providers and the sign-in notify
// never reaches the guard's instance → login bounces to /login?returnUrl=...
builder.Services.AddSingleton<TitleService>();
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddSingleton<ThemeState>();

// ── Offline-first local store + sync ─────────────────────────────────────────
builder.Services.AddSingleton<InMemoryLocalStore>();
builder.Services.AddSingleton<SyncStatusService>();
builder.Services.AddSingleton<SyncCoordinator>();
builder.Services.AddSingleton<JsLocalStore>();
builder.Services.AddSingleton<ResilientLocalStore>();
builder.Services.AddSingleton<ILocalStore>(sp => sp.GetRequiredService<ResilientLocalStore>());
builder.Services.AddSingleton<LocalMutator>();
builder.Services.AddSingleton<OfflineHttpHandler>();
builder.Services.AddSingleton<LazyOfflineHandler>();
builder.Services.AddSingleton<SyncEngine>(sp => new SyncEngine(
    () => sp.GetRequiredService<HttpClient>(),
    sp.GetRequiredService<ILocalStore>(),
    sp.GetRequiredService<SyncStatusService>()));

builder.Services.AddSingleton<IAccountManagement, JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddSingleton(sp =>
{
	var baseAddress = builder.Configuration["HealthspanApiBaseAddress"] ?? "https://localhost:7278";
	var timeoutSeconds = int.TryParse(builder.Configuration["HealthspanApiTimeoutSeconds"], out var configuredTimeoutSeconds)
		? Math.Max(30, configuredTimeoutSeconds)
		: 300;

	var handler = sp.GetRequiredService<LazyOfflineHandler>();
	return new HttpClient(handler)
	{
		BaseAddress = new Uri(baseAddress),
		Timeout = TimeSpan.FromSeconds(timeoutSeconds)
	};
});
builder.Services.AddMudServices();
builder.Services.AddSingleton<HealthspanApiClient>();

await builder.Build().RunAsync();
