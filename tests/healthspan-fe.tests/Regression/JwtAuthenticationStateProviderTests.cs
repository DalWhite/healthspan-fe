using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using FluentAssertions;
using Healthspan.Authentication;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class JwtAuthenticationStateProviderTests
{
    [Fact]
    public async Task Google_Login_Confirms_Authenticated_State_Before_Returning_Success()
    {
        string? storedAuthToken = null;
        string? storedRefreshToken = null;
        var localStorage = Substitute.For<ILocalStorageService>();
        localStorage.GetItemAsync<string>("authToken").Returns(_ => storedAuthToken);
        localStorage.GetItemAsync<string>("refreshToken").Returns(_ => storedRefreshToken);
        localStorage.SetItemAsync("authToken", Arg.Any<string>()).Returns(callInfo =>
        {
            storedAuthToken = callInfo.ArgAt<string>(1);
            return ValueTask.CompletedTask;
        });
        localStorage.SetItemAsync("refreshToken", Arg.Any<string>()).Returns(callInfo =>
        {
            storedRefreshToken = callInfo.ArgAt<string>(1);
            return ValueTask.CompletedTask;
        });

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, "https://localhost:7278/auth/google/exchange")
            .Respond("application/json", """
                {
                    "accessToken": "google-access-token",
                    "refreshToken": "google-refresh-token",
                    "expiresIn": 3600
                }
            """);
        var userInfoRequest = mockHttp.When(HttpMethod.Get, "https://localhost:7278/manage/info")
            .Respond("application/json", """
                {
                    "email": "google-user@healthspan.dev",
                    "claims": {}
                }
            """);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");
        var provider = new JwtAuthenticationStateProvider(httpClient, localStorage);

        var result = await provider.LoginWithGoogleAsync("google-id-token");

        result.Succeeded.Should().BeTrue();
        mockHttp.GetMatchCount(userInfoRequest).Should().Be(1);
        httpClient.DefaultRequestHeaders.Authorization?.Parameter.Should().Be("google-access-token");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ClearsAuthorizationHeader_WhenRefreshFails()
    {
        var localStorage = Substitute.For<ILocalStorageService>();
        localStorage.GetItemAsync<string>("authToken").Returns("expired-token");
        localStorage.GetItemAsync<string>("refreshToken").Returns("expired-refresh-token");

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, "https://localhost:7278/manage/info")
            .Respond(HttpStatusCode.Unauthorized);
        mockHttp.When(HttpMethod.Post, "https://localhost:7278/refresh")
            .Respond(HttpStatusCode.Unauthorized);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost:7278/");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "expired-token");

        var provider = new JwtAuthenticationStateProvider(httpClient, localStorage);

        var authenticationState = await provider.GetAuthenticationStateAsync();

        authenticationState.User.Identity?.IsAuthenticated.Should().BeFalse();
        httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
        await localStorage.Received().RemoveItemAsync("authToken");
        await localStorage.Received().RemoveItemAsync("refreshToken");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ClearsAuthorizationHeader_WhenTokenIsMissing()
    {
        var localStorage = Substitute.For<ILocalStorageService>();
        localStorage.GetItemAsync<string>("authToken").Returns((string?)null);

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7278/")
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stale-token");

        var provider = new JwtAuthenticationStateProvider(httpClient, localStorage);

        var authenticationState = await provider.GetAuthenticationStateAsync();

        authenticationState.User.Identity?.IsAuthenticated.Should().BeFalse();
        httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }
}
