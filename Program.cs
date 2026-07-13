using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using healthspan_fe_wasm_pwa;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Healthspan.Authentication;
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

builder.Services.AddScoped<IAccountManagement, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddScoped(_ =>
{
	var baseAddress = builder.Configuration["HealthspanApiBaseAddress"] ?? "https://localhost:7278";
	var timeoutSeconds = int.TryParse(builder.Configuration["HealthspanApiTimeoutSeconds"], out var configuredTimeoutSeconds)
		? Math.Max(30, configuredTimeoutSeconds)
		: 300;

	return new HttpClient
	{
		BaseAddress = new Uri(baseAddress),
		Timeout = TimeSpan.FromSeconds(timeoutSeconds)
	};
});
builder.Services.AddMudServices();
builder.Services.AddScoped<HealthspanApiClient>();

await builder.Build().RunAsync();
