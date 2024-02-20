using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using healthspan_fe_wasm_pwa;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Healthspan.Authentication;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<TitleService>();

builder.Services.AddScoped<IAccountManagement, JwtAuthenticationStateProvider>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["HealthspanApiBaseAddress"] ?? "https://localhost:7278") });
builder.Services.AddMudServices();

await builder.Build().RunAsync();
