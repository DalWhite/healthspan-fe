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

            if (result.IsSuccessStatusCode)
            {
                await ExtractAccessTokenAndStoreInLocalStorage(result);
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

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

    private async Task ExtractAccessTokenAndStoreInLocalStorage(HttpResponseMessage result)
    {
        var responseContent = await result.Content.ReadAsStringAsync();
        var tokenObject = JsonSerializer.Deserialize<AuthLoginResponse>(responseContent);
        if (tokenObject?.accessToken is not null)
        {
            await _localStorage.SetItemAsync("authToken", tokenObject.accessToken);
            await _localStorage.SetItemAsync("refreshToken", tokenObject.refreshToken);
        }
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrWhiteSpace(savedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
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
                throw new Exception("refresh failed");
            }

            savedToken = await _localStorage.GetItemAsync<string>("authToken");
            user = await GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(savedToken, user);
        }

        return new AuthenticationState(user);
    }

    private async Task<ClaimsPrincipal> GetUserInfoAndBuildClaimsIdentityUserAndSetAsAuthenticatedIfSuccess(string? savedToken, ClaimsPrincipal user)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
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
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
        await _httpClient.PostAsync("Logout", null);
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        await GetAuthenticationStateAsync();
        return _authenticated;
    }
    public async Task<FormResult> RefreshToken()
    {
        var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
        var refreshResult = await _httpClient.PostAsJsonAsync(
            "refresh", new
            {
                refreshToken
            });

        if (!refreshResult.IsSuccessStatusCode)
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            throw new ApplicationException("Something went wrong during the refresh token action, login out");
        }
        var refreshContent = await refreshResult.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthLoginResponse>(refreshContent);

        await _localStorage.SetItemAsync("authToken", result?.accessToken);
        await _localStorage.SetItemAsync("refreshToken", result?.refreshToken);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result?.accessToken);
        return new FormResult() { Succeeded = true };
    }
}
public class AuthLoginResponse
{
    public string? tokenType { get; set; }
    public string? accessToken { get; set; }
    public int expiresIn { get; set; }
    public string? refreshToken { get; set; }
}