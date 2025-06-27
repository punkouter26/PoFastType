using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using PoFastType.Client;
using PoFastType.Client.Services;
using Radzen;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddRadzenComponents();

// Configure authentication state provider
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IAccessTokenProvider, DevelopmentAccessTokenProvider>();
builder.Services.AddScoped<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddHttpClient("PoFastType.Api", client => client.BaseAddress = new Uri("https://localhost:5001/"))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("PoFastType.Api"));

// Register user service
builder.Services.AddScoped<IUserService, UserService>();

await builder.Build().RunAsync();

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // In development, create a mock authenticated user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
            new Claim(ClaimTypes.Name, "Development User"),
            new Claim(ClaimTypes.Email, "dev@pofasttype.com"),
            new Claim("identity_type", "development")
        };
        
        var identity = new ClaimsIdentity(claims, "Development");
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }
}

public class DevelopmentAccessTokenProvider : IAccessTokenProvider
{
    public ValueTask<AccessTokenResult> RequestAccessToken()
    {
        var token = new AccessToken { Value = "dummy_token", Expires = DateTimeOffset.Now.AddHours(1) };
        return ValueTask.FromResult(new AccessTokenResult(AccessTokenResultStatus.Success, token, "dummy_token", null));
    }

    public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        var token = new AccessToken { Value = "dummy_token", Expires = DateTimeOffset.Now.AddHours(1) };
        return ValueTask.FromResult(new AccessTokenResult(AccessTokenResultStatus.Success, token, "dummy_token", null));
    }
}
