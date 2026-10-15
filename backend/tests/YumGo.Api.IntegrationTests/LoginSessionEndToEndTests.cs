using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Api.IntegrationTests;

public sealed class LoginSessionEndToEndTests
{
    [Fact]
    public async Task Login_issues_access_jwt_and_refresh_session_with_48_hour_absolute_expiry()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_api_login_session_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();

        var signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString(),
                    ["Authentication:Jwt:Issuer"] = "YumGo.Tests",
                    ["Authentication:Jwt:Audience"] = "YumGo.Tests.Client",
                    ["Authentication:Jwt:SigningKey"] = signingKey,
                    ["Authentication:Jwt:AccessTokenLifetimeMinutes"] = "15"
                }));
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var registrationResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "login-session@example.com",
            password = "Long!Pass1",
            fullName = "Login Session"
        });
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);
        using var registrationBody = await JsonDocument.ParseAsync(await registrationResponse.Content.ReadAsStreamAsync());
        var customerId = registrationBody.RootElement.GetProperty("data").GetProperty("id").GetString();

        var loginStartedAt = DateTimeOffset.UtcNow;
        using var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "LOGIN-SESSION@EXAMPLE.COM",
            password = "Long!Pass1"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var responseBody = await JsonDocument.ParseAsync(await loginResponse.Content.ReadAsStreamAsync());
        var data = responseBody.RootElement.GetProperty("data");
        var accessToken = data.GetProperty("accessToken").GetString();
        var refreshToken = data.GetProperty("refreshToken").GetString();
        var accessExpiresAt = data.GetProperty("accessTokenExpiresAt").GetDateTimeOffset();
        var refreshExpiresAt = data.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal(3, accessToken!.Split('.').Length);
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
        Assert.True(accessExpiresAt > loginStartedAt);
        Assert.True(accessExpiresAt <= loginStartedAt.AddMinutes(16));
        Assert.InRange(
            refreshExpiresAt,
            loginStartedAt.AddHours(48).AddSeconds(-30),
            loginStartedAt.AddHours(48).AddSeconds(30));

        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = tokenHandler.ValidateToken(
            accessToken!,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "YumGo.Tests",
                ValidateAudience = true,
                ValidAudience = "YumGo.Tests.Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = "role",
                NameClaimType = "sub"
            },
            out _);
        Assert.Equal(customerId, principal.FindFirst("sub")?.Value);
        Assert.Contains("customer", principal.FindAll("role").Select(claim => claim.Value));

        var storedTokenHash = await dbContext.Database
            .SqlQueryRaw<string>("SELECT token_hash AS \"Value\" FROM refresh_tokens")
            .SingleAsync();
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken!)));
        Assert.Equal(expectedHash, storedTokenHash);
    }
}
