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
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<TitleService>();
builder.Services.AddScoped<AppStateService>();
builder.Services.AddScoped<ThemeState>();

// ── Offline-first local store + sync ─────────────────────────────────────────
builder.Services.AddSingleton<InMemoryLocalStore>();
builder.Services.AddSingleton<SyncStatusService>();
builder.Services.AddSingleton<SyncCoordinator>();
builder.Services.AddScoped<JsLocalStore>();
builder.Services.AddScoped<ResilientLocalStore>();
builder.Services.AddScoped<ILocalStore>(sp => sp.GetRequiredService<ResilientLocalStore>());
builder.Services.AddScoped<LocalMutator>();
builder.Services.AddScoped<OfflineHttpHandler>();
builder.Services.AddScoped<LazyOfflineHandler>();
builder.Services.AddScoped<SyncEngine>(sp => new SyncEngine(
    () => sp.GetRequiredService<HttpClient>(),
    sp.GetRequiredService<ILocalStore>(),
    sp.GetRequiredService<SyncStatusService>()));

builder.Services.AddScoped<IAccountManagement, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddScoped(sp =>
{
	var baseAddress = builder.Configuration["HealthspanApiBaseAddress"] ?? "https://localhost:7278";
	var timeoutSeconds = int.TryParse(builder.Configuration["HealthspanApiTimeoutSeconds"], out var configuredTimeoutSeconds)
		? Math.Max(30, configuredTimeoutSeconds)
		: 300;

	return new HttpClient(
		new LazyOfflineHandler(sp)
		{
			InnerHandler = new HttpClientHandler()
		})
	{
		BaseAddress = new Uri(baseAddress),
		Timeout = TimeSpan.FromSeconds(timeoutSeconds)
	};
});
builder.Services.AddMudServices();
builder.Services.AddScoped<HealthspanApiClient>();

await builder.Build().RunAsync();
