using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using System.Net.Http.Headers;
using Healthspan.Authentication.Models;

namespace Healthspan.Authentication;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider, IAccountManagement
{
    private readonly JsonSerializerOptions jsonSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    private readonly IServiceProvider _services;
    private HttpClient? _httpClient;
    private ILocalStorageService? _localStorage;
    private HttpClient Http => _httpClient ??= _services.GetRequiredService<HttpClient>();
    private ILocalStorageService Storage => _localStorage ??= _services.GetRequiredService<ILocalStorageService>();
    private bool _authenticated = false;

    // Cached auth state: guard evaluations (AuthorizeRouteView, MainLayout,
    // CascadingAuthenticationState re-cascade) hit this many times per
    // navigation. Without a cache each evaluation re-fetches manage/info,
    // which is slow enough that the guard can render NotAuthorized with a
    // stale cascaded task and bounce a just-completed login to /login.
    private AuthenticationState? _cachedState;
    private DateTime _cachedStateAt;
    private static readonly TimeSpan AuthStateCacheTtl = TimeSpan.FromSeconds(60);
    private readonly ClaimsPrincipal Unauthenticated =
        new(new ClaimsIdentity());
    // Singleton-safe: HttpClient and ILocalStorageService are scoped, so they
    // are resolved lazily from the provider instead of constructor-injected
    // (a singleton cannot constructor-inject scoped services).
    public JwtAuthenticationStateProvider(IServiceProvider services)
    {
        _services = services;
    }

    // Test-only compatibility: direct injection of the two lazy dependencies.
    public JwtAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _services = null!;
        _httpClient = httpClient;
        _localStorage = localStorage;
    }
    public async Task<FormResult> RegisterAsync(string email, string password)
    {
        string[] defaultDetail = ["An unknown error prevented registration from succeeding."];

        try
        {
            var result = await Http.PostAsJsonAsync(
                "register", new
                {
                    email,
                    password
                });

            if (result.IsSuccessStatusCode)
            {
                return new FormResult { Succeeded = true };
            }

            var details = await result.Content.ReadAsStringAsync();
            var problemDetails = JsonDocument.Parse(details);
            var errors = new List<string>();
            var errorList = problemDetails.RootElement.GetProperty("errors");

            foreach (var errorEntry in errorList.EnumerateObject())
            {
                if (errorEntry.Value.ValueKind == JsonValueKind.String)
                {
                    errors.Add(errorEntry.Value.GetString()!);
                }
                else if (errorEntry.Value.ValueKind == JsonValueKind.Array)
                {
                    errors.AddRange(
                        errorEntry.Value.EnumerateArray().Select(
                            e => e.GetString() ?? string.Empty)
                        .Where(e => !string.IsNullOrEmpty(e)));
                }
            }

            return new FormResult
            {
                Succeeded = false,
                ErrorList = problemDetails == null ? defaultDetail : [.. errors]
            };
        }
        catch { }

        return new FormResult
        {
            Succeeded = false,
            ErrorList = defaultDetail
        };
    }
    public async Task<FormResult> LoginAsync(string email, string password)
    {
        try
        {
            await Storage.SetItemAsync("offlineEmail", email);
            var result = await Http.PostAsJsonAsync(
                "login", new
                {
                    email,
                    password
                });

            if (result.IsSuccessStatusCode && await CompleteSignInAsync(result))
            {
                return new FormResult { Succeeded = true };
            }

        }
        catch (Exception ex)
        {
        }

        return new FormResult
        {
            Succeeded = false,
            ErrorList = ["Invalid email and/or password."]
        };
    }

    public async Task<FormResult> LoginWithGoogleAsync(string idToken)
    {
        const string defaultError = "Unable to sign in with Google right now.";

        try
        {
            var response = await Http.PostAsJsonAsync(
                "auth/google/exchange",
                new
                {
                    idToken
                });

            if (response.IsSuccessStatusCode && await CompleteSignInAsync(response))
            {
                return new FormResult { Succeeded = true };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return new FormResult
            {
                Succeeded = false,
                ErrorList = ExtractProblemDetails(responseContent, defaultError)
            };
        }
        catch
        {
            return new FormResult
            {
                Succeeded = false,
                ErrorList = [defaultError]
            };
        }
    }

    private async Task<bool> CompleteSignInAsync(HttpResponseMessage response)
    {
        if (!await ExtractAccessTokenAndStoreInLocalStorage(response))
        {
            return false;
        }

        // Fresh sign-in: bypass any cached (possibly unauthenticated) state.
        _cachedState = null;
        var authenticationState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        // Give the Router/AuthorizeRouteView a beat to consume the new state
        // before the caller navigates; otherwise the guard still sees the old
        // unauthenticated task and bounces back to /login.
        await Task.Delay(250);
        return authenticationState.User.Identity?.IsAuthenticated == true;
    }

    private async Task<bool> ExtractAccessTokenAndStoreInLocalStorage(HttpResponseMessage result)
    {
        var responseContent = await result.Content.ReadAsStringAsync();
        var tokenObject = JsonSerializer.Deserialize<AuthLoginResponse>(responseContent);
        if (string.IsNullOrWhiteSpace(tokenObject?.accessToken))
        {
            return false;
        }

        await Storage.SetItemAsync("authToken", tokenObject.accessToken);
        await Storage.SetItemAsync("refreshToken", tokenObject.refreshToken);
        // The access token is an opaque protected ticket, not a JWT: remember
        // its expiry locally so offline auth can trust it while it is fresh.
        await Storage.SetItemAsync("authTokenExpiresAt",
            DateTimeOffset.UtcNow.AddSeconds(tokenObject.expiresIn > 0 ? tokenObject.expiresIn : 3600).ToUnixTimeSeconds());
        var id = await Storage.GetItemAsync<string>("offlineEmail");
        if (!string.IsNullOrWhiteSpace(id))
        {
            await Storage.SetItemAsync("offlineEmail", id);
        }
        SetAuthorizationHeader(tokenObject.accessToken);
        return true;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cachedState != null &&
            DateTime.UtcNow - _cachedStateAt < AuthStateCacheTtl)
        {
            return _cachedState;
        }

        var savedToken = await Storage.GetItemAsync<string>("authToken");
        if (string.IsNullOrWhiteSpace(savedToken))
        {
            _authenticated = false;
            SetAuthorizationHeader(null);
            return CacheUnauthenticated();
        }

        _authenticated = false;

        var user = Unauthenticated;

        try
        {
            user = await GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(savedToken, user);
        }
        catch (Exception netEx)
        {
            // Offline-first: any manage/info failure (fetch abort, 5xx, network)
            // falls back to the locally-decoded token while it is unexpired.
            var offlineUser = await BuildOfflinePrincipal(savedToken);
            if (offlineUser != null)
            {
                Console.WriteLine($"[offline] auth: trusting unexpired token ({netEx.GetType().Name})");
                _authenticated = true;
                return CacheAuthenticated(new AuthenticationState(offlineUser));
            }

            try
            {
                var refreshResult = await RefreshToken();
                if (!refreshResult.Succeeded)
                {
                    SetAuthorizationHeader(null);
                    return CacheUnauthenticated();
                }
            }
            catch (Exception refreshEx)
            {
                Console.WriteLine($"[offline] refresh failed: {refreshEx.Message}");
                SetAuthorizationHeader(null);
                return CacheUnauthenticated();
            }

            savedToken = await Storage.GetItemAsync<string>("authToken");
            user = await GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(savedToken, user);
        }

        var state = new AuthenticationState(user);
        return _authenticated ? CacheAuthenticated(state) : CacheUnauthenticated();
    }

    private AuthenticationState CacheAuthenticated(AuthenticationState state)
    {
        _cachedState = state;
        _cachedStateAt = DateTime.UtcNow;
        return state;
    }

    private AuthenticationState CacheUnauthenticated()
    {
        _cachedState = null;
        return CreateUnauthenticatedState();
    }

    // Offline auth: the access token is an opaque protected ticket, so we
    // trust it while the expiry recorded at sign-in has not passed.
    private async Task<ClaimsPrincipal?> BuildOfflinePrincipal(string token)
    {
        try
        {
            var expiresAt = await Storage.GetItemAsync<long>("authTokenExpiresAt");
            if (expiresAt <= 0 || expiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }

            var email = await Storage.GetItemAsync<string>("offlineEmail") ?? "offline-user";

            var id = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.Email, email) },
                nameof(JwtAuthenticationStateProvider));
            SetAuthorizationHeader(token);
            return new ClaimsPrincipal(id);
        }
        catch
        {
            return null;
        }
    }

    private async Task<ClaimsPrincipal> GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(string? savedToken, ClaimsPrincipal user)
    {
        SetAuthorizationHeader(savedToken);
        var userResponse = await Http.GetAsync("manage/info");
        userResponse.EnsureSuccessStatusCode();
        var userJson = await userResponse.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson, jsonSerializerOptions);

        if (userInfo != null)
        {
            var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userInfo.Email),
                        new(ClaimTypes.Email, userInfo.Email)
                    };

            claims.AddRange(
                userInfo.Claims.Where(c => c.Key != ClaimTypes.Name && c.Key != ClaimTypes.Email)
                    .Select(c => new Claim(c.Key, c.Value)));

            var id = new ClaimsIdentity(claims, nameof(JwtAuthenticationStateProvider));
            user = new ClaimsPrincipal(id);
            _authenticated = true;
        }

        return user;
    }

    public async Task LogoutAsync()
    {
        var savedToken = await Storage.GetItemAsync<string>("authToken");
        SetAuthorizationHeader(savedToken);
        await Http.PostAsync("Logout", null);
        await Storage.RemoveItemAsync("authToken");
        await Storage.RemoveItemAsync("refreshToken");
        SetAuthorizationHeader(null);
        _authenticated = false;
        _cachedState = null;
        NotifyAuthenticationStateChanged(Task.FromResult(CreateUnauthenticatedState()));
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        await GetAuthenticationStateAsync();
        return _authenticated;
    }
    public async Task<FormResult> RefreshToken()
    {
        var refreshToken = await Storage.GetItemAsync<string>("refreshToken");
        SetAuthorizationHeader(null);
        var refreshResult = await Http.PostAsJsonAsync(
            "refresh", new
            {
                refreshToken
            });

        if (!refreshResult.IsSuccessStatusCode)
        {
            await Storage.RemoveItemAsync("authToken");
            await Storage.RemoveItemAsync("refreshToken");
            SetAuthorizationHeader(null);
            _authenticated = false;
            _cachedState = null;
            NotifyAuthenticationStateChanged(Task.FromResult(CreateUnauthenticatedState()));
            return new FormResult
            {
                Succeeded = false,
                ErrorList = ["error refreshing token"]
            };
        }
        var refreshContent = await refreshResult.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthLoginResponse>(refreshContent);

        await Storage.SetItemAsync("authToken", result?.accessToken);
        await Storage.SetItemAsync("refreshToken", result?.refreshToken);

        SetAuthorizationHeader(result?.accessToken);
        return new FormResult() { Succeeded = true };
    }

    private void SetAuthorizationHeader(string? accessToken)
    {
        Http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private AuthenticationState CreateUnauthenticatedState()
        => new(Unauthenticated);

    private static string[] ExtractProblemDetails(string? rawResponse, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            return [fallback];
        }

        try
        {
            using var document = JsonDocument.Parse(rawResponse);

            if (document.RootElement.TryGetProperty("errors", out var errorsElement) &&
                errorsElement.ValueKind == JsonValueKind.Object)
            {
                var errors = new List<string>();
                foreach (var errorEntry in errorsElement.EnumerateObject())
                {
                    if (errorEntry.Value.ValueKind == JsonValueKind.String)
                    {
                        errors.Add(errorEntry.Value.GetString() ?? string.Empty);
                    }
                    else if (errorEntry.Value.ValueKind == JsonValueKind.Array)
                    {
                        errors.AddRange(
                            errorEntry.Value.EnumerateArray()
                                .Select(e => e.GetString() ?? string.Empty)
                                .Where(e => !string.IsNullOrWhiteSpace(e)));
                    }
                }

                if (errors.Count > 0)
                {
                    return [.. errors];
                }
            }

            if (document.RootElement.TryGetProperty("detail", out var detailElement) &&
                detailElement.ValueKind == JsonValueKind.String)
            {
                return [detailElement.GetString() ?? fallback];
            }

            if (document.RootElement.TryGetProperty("title", out var titleElement) &&
                titleElement.ValueKind == JsonValueKind.String)
            {
                return [titleElement.GetString() ?? fallback];
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return [fallback];
    }
}
public class AuthLoginResponse
{
    public string? tokenType { get; set; }
    public string? accessToken { get; set; }
    public int expiresIn { get; set; }
    public string? refreshToken { get; set; }
}
