using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YumGo.Application.Identity;
using YumGo.Domain.Common;

namespace YumGo.Api.IntegrationTests;

public sealed class RestaurantWorkflowEndpointsTests
{
    [Fact]
    public async Task Restaurant_workflow_routes_require_authentication()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationStore>();
                services.AddSingleton<IAuthenticationStore, ActiveSessionStore>();
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var restaurantId = NanoId.New().Value;
        var branchId = NanoId.New().Value;
        var userId = NanoId.New().Value;
        var requests = new (HttpMethod Method, string Path, object Body)[]
        {
            (HttpMethod.Post, $"/api/v1/admin/restaurants/{restaurantId}/review", new { decision = "approve" }),
            (HttpMethod.Patch, $"/api/v1/restaurant/restaurants/{restaurantId}/branches/{branchId}/status", new { status = "open" }),
            (HttpMethod.Put, $"/api/v1/restaurant/restaurants/{restaurantId}/staff/{userId}/roles", new { roles = new[] { "staff" } })
        };

        foreach (var (method, path, body) in requests)
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(body)
            };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private sealed class ActiveSessionStore : IAuthenticationStore
    {
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateRefreshTokenAsync(string id, string userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RefreshTokenSession?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RotateRefreshTokenAsync(string oldTokenId, string oldTokenHash, string newTokenId, string userId, string newTokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RevokeRefreshTokenDescendantsAsync(string tokenId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsSessionActiveAsync(string userId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task RecordAuthenticationAuditAsync(string? actorUserId, string action, string? ipAddress, string? userAgent, DateTimeOffset createdAt, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
