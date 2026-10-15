using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace YumGo.Api.IntegrationTests;

internal static class TestJwtConfiguration
{
    public static string CreateSigningKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static void Apply(IWebHostBuilder builder, string? signingKey = null)
    {
        signingKey ??= CreateSigningKey();
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Jwt:Issuer"] = "YumGo.Tests",
                ["Authentication:Jwt:Audience"] = "YumGo.Tests.Client",
                ["Authentication:Jwt:SigningKey"] = signingKey,
                ["Authentication:Jwt:AccessTokenLifetimeMinutes"] = "15"
            }));
    }
}
