using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobbPilot.Api.IntegrationTests.Infrastructure;
using Shouldly;

namespace JobbPilot.Api.IntegrationTests.Auth;

[Collection("Api")]
public class RefreshReplayTests(ApiFactory factory)
{
    // HandleCookies=false so we control cookie headers manually.
    // The Secure cookie flag prevents the HttpClient cookie container from
    // sending cookies over http (the WebApplicationFactory test server scheme).
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

    [Fact]
    public async Task Refresh_ReusedRevokedToken_RevokesEntireChain()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = $"replay-{Guid.NewGuid()}@example.com";
        const string password = "S3kretlosen123!";

        // Register → Set-Cookie: jobbpilot-refresh=<token1>
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password, displayName = "Replay User" }, ct);
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token1 = ExtractRefreshCookieValue(registerResponse);
        token1.ShouldNotBeNullOrWhiteSpace();

        // Refresh with token1 → rotation: token1 revoked, token2 issued
        var token2 = await RefreshWithCookieAsync(token1!, ct);
        token2.ShouldNotBeNullOrWhiteSpace();

        // Refresh with token2 → rotation: token2 revoked, token3 issued
        var token3 = await RefreshWithCookieAsync(token2!, ct);
        token3.ShouldNotBeNullOrWhiteSpace();

        // REPLAY: reuse token1 (already revoked) → expect 401, chain revoked
        var replayRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replayRequest.Headers.Add("Cookie", $"jobbpilot-refresh={token1}");
        var replayResponse = await _client.SendAsync(replayRequest, ct);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Entire chain must now be revoked: token3 (the last active token) must also fail
        var postChainRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        postChainRequest.Headers.Add("Cookie", $"jobbpilot-refresh={token3}");
        var postChainResponse = await _client.SendAsync(postChainRequest, ct);
        postChainResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<string?> RefreshWithCookieAsync(string cookieValue, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request.Headers.Add("Cookie", $"jobbpilot-refresh={cookieValue}");
        var response = await _client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return ExtractRefreshCookieValue(response);
    }

    private static string? ExtractRefreshCookieValue(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        foreach (var cookie in cookies)
        {
            if (!cookie.StartsWith("jobbpilot-refresh=", StringComparison.Ordinal))
                continue;
            var value = cookie["jobbpilot-refresh=".Length..];
            var semicolonIndex = value.IndexOf(';');
            return semicolonIndex >= 0 ? value[..semicolonIndex] : value;
        }

        return null;
    }
}
