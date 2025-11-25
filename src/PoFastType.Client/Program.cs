using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PoFastType.Client;
using PoFastType.Client.Services;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddRadzenComponents();

// Get the API base address from configuration, with fallback logic
var apiBaseAddress = builder.Configuration["ApiBaseAddress"];
if (string.IsNullOrEmpty(apiBaseAddress))
{
    // In hosted mode, use the host environment base address (relative URLs)
    // In standalone mode, fall back to explicit URLs
    apiBaseAddress = builder.HostEnvironment.BaseAddress;
}

// Add an anonymous HttpClient for public API calls (like game text, anonymous gameplay)
builder.Services.AddHttpClient("PoFastType.Api.Anonymous", client => client.BaseAddress = new Uri(apiBaseAddress));

// Register the default HttpClient as the anonymous one for general use
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PoFastType.Api.Anonymous"));

// Register game state service for component communication
builder.Services.AddSingleton<GameStateService>();

await builder.Build().RunAsync();
