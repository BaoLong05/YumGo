using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace YumGo.Api.IntegrationTests;

public sealed class CurrentIdentityEndToEndTests
{
    [Fact]
    public async Task Authenticated_user_can_read_current_identity_and_roles()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        var userId = await host.RegisterCustomerAsync("identity-session@example.com");
        using var loginResponse = await host.Client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = "identity-session@example.com", password = "Long!Pass1" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = loginJson.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var identityResponse = await host.Client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, identityResponse.StatusCode);

        using var identityJson = JsonDocument.Parse(await identityResponse.Content.ReadAsStringAsync());
        var data = identityJson.RootElement.GetProperty("data");
        Assert.Equal(userId, data.GetProperty("id").GetString());
        Assert.Contains("customer", data.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
    }
}
