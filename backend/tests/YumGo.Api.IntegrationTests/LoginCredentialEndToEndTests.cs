using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace YumGo.Api.IntegrationTests;

public sealed class LoginCredentialEndToEndTests
{
    [Fact]
    public async Task Login_does_not_disclose_whether_an_email_exists()
    {
        await using var host = await AuthApiTestHost.StartAsync();
        await host.RegisterCustomerAsync("known-login@example.com");

        using var wrongPasswordResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "known-login@example.com",
            password = "Wrong!Pass9"
        });
        using var unknownEmailResponse = await host.Client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "unknown-login@example.com",
            password = "Wrong!Pass9"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);

        using var wrongPasswordBody = await JsonDocument.ParseAsync(await wrongPasswordResponse.Content.ReadAsStreamAsync());
        using var unknownEmailBody = await JsonDocument.ParseAsync(await unknownEmailResponse.Content.ReadAsStreamAsync());
        Assert.Equal(
            wrongPasswordBody.RootElement.GetProperty("title").GetString(),
            unknownEmailBody.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            wrongPasswordBody.RootElement.GetProperty("detail").GetString(),
            unknownEmailBody.RootElement.GetProperty("detail").GetString());

        var failureAuditCount = await host.DbContext.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS "Value"
            FROM audit_logs
            WHERE action = 'auth.login.failed'
              AND actor_user_id IS NULL
              AND before_json IS NULL
              AND after_json IS NULL
            """).SingleAsync();
        Assert.Equal(2, failureAuditCount);
    }
}
