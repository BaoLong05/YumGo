using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace YumGo.Api.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_endpoint_returns_success()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => TestJwtConfiguration.Apply(builder));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
