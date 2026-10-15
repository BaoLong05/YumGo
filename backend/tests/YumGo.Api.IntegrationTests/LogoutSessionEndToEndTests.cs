using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace YumGo.Api.IntegrationTests;

public sealed class LogoutSessionEndToEndTests
{
    [Fact]
    public async Task Logout_revokes_the_current_session()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("logout-session@example.com");

        using var loginResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "logout-session@example.com",
            password = "Long!Pass1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginBody = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var data = loginBody.RootElement.GetProperty("data");
        var accessToken = data.GetProperty("accessToken").GetString();
        var refreshToken = data.GetProperty("refreshToken").GetString();
        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var logoutResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var auditCount = await host.DbContext.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS "Value"
            FROM audit_logs
            WHERE action = 'auth.logout.succeeded'
              AND before_json IS NULL
              AND after_json IS NULL
            """).SingleAsync();
        Assert.Equal(1, auditCount);

        using var staleAccessResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/logout", new
        {
            refreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, staleAccessResponse.StatusCode);

        host.Client.DefaultRequestHeaders.Authorization = null;
        using var staleRefreshResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, staleRefreshResponse.StatusCode);
    }
}
