using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace YumGo.Api.IntegrationTests;

public sealed class RefreshSessionEndToEndTests
{
    [Fact]
    public async Task Refresh_keeps_the_original_48_hour_absolute_deadline()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("refresh-session@example.com");

        using var loginResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "refresh-session@example.com",
            password = "Long!Pass1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginBody = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var initialData = loginBody.RootElement.GetProperty("data");
        var initialRefreshToken = initialData.GetProperty("refreshToken").GetString();
        var originalDeadline = initialData.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset();

        using var refreshResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = initialRefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        using var refreshBody = await JsonDocument.ParseAsync(await refreshResponse.Content.ReadAsStreamAsync());
        var refreshedData = refreshBody.RootElement.GetProperty("data");
        Assert.NotEqual(initialRefreshToken, refreshedData.GetProperty("refreshToken").GetString());
        Assert.InRange(
            refreshedData.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset(),
            originalDeadline.AddSeconds(-2),
            originalDeadline.AddSeconds(2));
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_the_successor()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("refresh-replay@example.com");

        using var loginResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "refresh-replay@example.com",
            password = "Long!Pass1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginBody = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var originalToken = loginBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

        using var rotationResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = originalToken
        });
        Assert.Equal(HttpStatusCode.OK, rotationResponse.StatusCode);
        using var rotationBody = await JsonDocument.ParseAsync(await rotationResponse.Content.ReadAsStreamAsync());
        var successorToken = rotationBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

        using var replayResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = originalToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        using var successorResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = successorToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, successorResponse.StatusCode);
    }

    [Fact]
    public async Task Concurrent_refresh_requests_cannot_leave_a_successor_session_active()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("refresh-concurrent@example.com");

        using var loginResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "refresh-concurrent@example.com",
            password = "Long!Pass1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginBody = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var originalToken = loginBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

        var responses = await Task.WhenAll(
            host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = originalToken }),
            host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = originalToken }));
        try
        {
            var successfulResponse = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
            using var successfulBody = await JsonDocument.ParseAsync(await successfulResponse.Content.ReadAsStreamAsync());
            var successorToken = successfulBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

            using var successorResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
            {
                refreshToken = successorToken
            });
            Assert.Equal(HttpStatusCode.Unauthorized, successorResponse.StatusCode);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }
}
