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
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private bool _authenticated = false;
    private readonly ClaimsPrincipal Unauthenticated =
        new(new ClaimsIdentity());
    public JwtAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }
    public async Task<FormResult> RegisterAsync(string email, string password)
    {
        string[] defaultDetail = ["An unknown error prevented registration from succeeding."];

        try
        {
            var result = await _httpClient.PostAsJsonAsync(
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
            var result = await _httpClient.PostAsJsonAsync(
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
        catch { }

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
            var response = await _httpClient.PostAsJsonAsync(
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

        var authenticationState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
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

        await _localStorage.SetItemAsync("authToken", tokenObject.accessToken);
        await _localStorage.SetItemAsync("refreshToken", tokenObject.refreshToken);
        SetAuthorizationHeader(tokenObject.accessToken);
        return true;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrWhiteSpace(savedToken))
        {
            _authenticated = false;
            SetAuthorizationHeader(null);
            return CreateUnauthenticatedState();
        }

        _authenticated = false;

        var user = Unauthenticated;

        try
        {
            user = await GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(savedToken, user);
        }
        catch
        {
            var refreshResult = await RefreshToken();
            if (!refreshResult.Succeeded)
            {
                SetAuthorizationHeader(null);
                return CreateUnauthenticatedState();
            }

            savedToken = await _localStorage.GetItemAsync<string>("authToken");
            user = await GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(savedToken, user);
        }

        return new AuthenticationState(user);
    }

    private async Task<ClaimsPrincipal> GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(string? savedToken, ClaimsPrincipal user)
    {
        SetAuthorizationHeader(savedToken);
        var userResponse = await _httpClient.GetAsync("manage/info");
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
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");
        SetAuthorizationHeader(savedToken);
        await _httpClient.PostAsync("Logout", null);
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        SetAuthorizationHeader(null);
        _authenticated = false;
        NotifyAuthenticationStateChanged(Task.FromResult(CreateUnauthenticatedState()));
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        await GetAuthenticationStateAsync();
        return _authenticated;
    }
    public async Task<FormResult> RefreshToken()
    {
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
        SetAuthorizationHeader(null);
        var refreshResult = await _httpClient.PostAsJsonAsync(
            "refresh", new
            {
                refreshToken
            });

        if (!refreshResult.IsSuccessStatusCode)
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            SetAuthorizationHeader(null);
            _authenticated = false;
            NotifyAuthenticationStateChanged(Task.FromResult(CreateUnauthenticatedState()));
            return new FormResult
            {
                Succeeded = false,
                ErrorList = ["error refreshing token"]
            };
        }
        var refreshContent = await refreshResult.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthLoginResponse>(refreshContent);

        await _localStorage.SetItemAsync("authToken", result?.accessToken);
        await _localStorage.SetItemAsync("refreshToken", result?.refreshToken);

        SetAuthorizationHeader(result?.accessToken);
        return new FormResult() { Succeeded = true };
    }

    private void SetAuthorizationHeader(string? accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(accessToken)
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
