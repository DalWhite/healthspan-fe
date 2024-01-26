using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using healthspan_fe_wasm_pwa;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorWasmAuth.Identity;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<JwtHandler>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddScoped(sp => (IAccountManagement)sp.GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5152") });
builder.Services.AddHttpClient("Auth",opt => opt.BaseAddress = new Uri("http://localhost:5152"))
    .AddHttpMessageHandler<JwtHandler>();
    
builder.Services.AddMudServices();

await builder.Build().RunAsync();
