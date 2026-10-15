using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace YumGo.Api.IntegrationTests;

public sealed class AuthenticationAuditEndToEndTests
{
    [Fact]
    public async Task Successful_login_records_an_audit_event_without_token_payloads()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("login-audit@example.com");
        host.Client.DefaultRequestHeaders.UserAgent.ParseAdd("YumGo.Auth.Tests");

        using var response = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "login-audit@example.com",
            password = "Long!Pass1"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var auditCount = await host.DbContext.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS "Value"
            FROM audit_logs
            WHERE action = 'auth.login.succeeded'
              AND resource_type = 'user'
              AND before_json IS NULL
              AND after_json IS NULL
              AND user_agent = 'YumGo.Auth.Tests'
            """).SingleAsync();
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task Successful_refresh_records_an_audit_event_without_token_payloads()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("refresh-audit@example.com");

        using var loginResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "refresh-audit@example.com",
            password = "Long!Pass1"
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginBody = await System.Text.Json.JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var refreshToken = loginBody.RootElement.GetProperty("data").GetProperty("refreshToken").GetString();

        host.Client.DefaultRequestHeaders.UserAgent.ParseAdd("YumGo.Auth.Tests");
        using var refreshResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(System.Net.HttpStatusCode.OK, refreshResponse.StatusCode);

        var auditCount = await host.DbContext.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS "Value"
            FROM audit_logs
            WHERE action = 'auth.refresh.rotated'
              AND resource_type = 'user'
              AND before_json IS NULL
              AND after_json IS NULL
              AND user_agent = 'YumGo.Auth.Tests'
            """).SingleAsync();
        Assert.Equal(1, auditCount);
    }
}
