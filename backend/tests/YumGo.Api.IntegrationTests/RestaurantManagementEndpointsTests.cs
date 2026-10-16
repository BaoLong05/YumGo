using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using YumGo.Application.Addresses;
using YumGo.Application.Identity;
using YumGo.Application.Restaurants;
using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;
using YumGo.Domain.Restaurants;

namespace YumGo.Api.IntegrationTests;

public sealed class RestaurantManagementEndpointsTests
{
    [Fact]
    public async Task Restaurant_management_derives_owner_from_session_and_enforces_scope()
    {
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        var store = new FakeRestaurantStore();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder, signingKey);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused" }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationStore>();
                services.AddSingleton<IAuthenticationStore, FakeAuthenticationStore>();
                services.RemoveAll<IRestaurantBranchStore>();
                services.AddSingleton<IRestaurantBranchStore>(store);
                services.RemoveAll<ICustomerAddressStore>();
                services.AddSingleton<ICustomerAddressStore, EmptyAddressStore>();
                services.RemoveAll<IGoogleGeocodingQuota>();
                services.AddSingleton<IGoogleGeocodingQuota, NoopQuota>();
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var anonymous = await client.GetAsync("/api/v1/restaurant/restaurants");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var ownerId = NanoId.New().Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(ownerId, signingKey));
        using var forgedOwner = await client.PostAsJsonAsync("/api/v1/restaurant/restaurants", new
        {
            name = "Pho House", description = "Family restaurant", ownerUserId = NanoId.New().Value
        });
        Assert.Equal(HttpStatusCode.BadRequest, forgedOwner.StatusCode);
        Assert.Null(store.CreatedRestaurant);

        using var createRestaurant = await client.PostAsJsonAsync("/api/v1/restaurant/restaurants", new
        {
            name = "  Pho House  ", description = "Family restaurant"
        });
        Assert.Equal(HttpStatusCode.Created, createRestaurant.StatusCode);
        using var restaurantJson = await System.Text.Json.JsonDocument.ParseAsync(await createRestaurant.Content.ReadAsStreamAsync());
        var restaurantData = restaurantJson.RootElement.GetProperty("data");
        var restaurantId = restaurantData.GetProperty("id").GetString()!;
        Assert.Equal("pending", restaurantData.GetProperty("status").GetString());
        Assert.Equal(ownerId, store.CreatedRestaurant?.OwnerUserId.Value);

        using var createBranch = await client.PostAsJsonAsync($"/api/v1/restaurant/restaurants/{restaurantId}/branches", new
        {
            name = "District 1", phone = "+84912345678", addressLine = "12 Nguyen Hue", ward = "Ben Nghe",
            district = "District 1", city = "HCMC", latitude = 10.7769m, longitude = 106.7009m,
            opensAt = "08:00:00", closesAt = "22:00:00"
        });
        Assert.Equal(HttpStatusCode.Created, createBranch.StatusCode);
        using var branchJson = await System.Text.Json.JsonDocument.ParseAsync(await createBranch.Content.ReadAsStreamAsync());
        Assert.Equal("draft", branchJson.RootElement.GetProperty("data").GetProperty("status").GetString());

        using var missingCoordinates = await client.PostAsJsonAsync($"/api/v1/restaurant/restaurants/{restaurantId}/branches", new
        {
            name = "District 2", phone = "+84912345678", addressLine = "12 Nguyen Hue", ward = "Ben Nghe",
            district = "District 1", city = "HCMC"
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingCoordinates.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(NanoId.New().Value, signingKey));
        using var foreignList = await client.GetAsync($"/api/v1/restaurant/restaurants/{restaurantId}/branches");
        Assert.Equal(HttpStatusCode.NotFound, foreignList.StatusCode);
        using var foreignCreate = await client.PostAsJsonAsync($"/api/v1/restaurant/restaurants/{restaurantId}/branches", new
        {
            name = "Stolen", phone = "+84912345678", addressLine = "12 Nguyen Hue", ward = "Ben Nghe",
            district = "District 1", city = "HCMC", latitude = 10m, longitude = 106m
        });
        Assert.Equal(HttpStatusCode.NotFound, foreignCreate.StatusCode);
    }

    private static string CreateToken(string userId, string signingKey)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(issuer: "YumGo.Tests", audience: "YumGo.Tests.Client",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId), new Claim("sid", NanoId.New().Value)],
            notBefore: now.AddMinutes(-1), expires: now.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(signingKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class FakeAuthenticationStore : IAuthenticationStore
    {
        public Task<AuthenticationAccount?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task CreateRefreshTokenAsync(string id, string userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<RefreshTokenSession?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RotateRefreshTokenAsync(string oldTokenId, string oldTokenHash, string newTokenId, string userId, string newTokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? ipAddress, string? userAgent, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RevokeRefreshTokenDescendantsAsync(string tokenId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsSessionActiveAsync(string userId, string sessionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task RecordAuthenticationAuditAsync(string? actorUserId, string action, string? ipAddress, string? userAgent, DateTimeOffset createdAt, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class EmptyAddressStore : ICustomerAddressStore
    {
        public Task<IReadOnlyList<CustomerAddress>> ListAsync(NanoId userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CustomerAddress>>([]);
        public Task<CustomerAddress?> FindAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) => Task.FromResult<CustomerAddress?>(null);
        public Task CreateAsync(CustomerAddress address, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> UpdateAsync(CustomerAddress address, bool? isDefaultOverride, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> DeleteAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> SetDefaultAsync(NanoId userId, NanoId addressId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class NoopQuota : IGoogleGeocodingQuota
    {
        public Task<bool> TryReserveAsync(int monthlyLimit, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeRestaurantStore : IRestaurantBranchStore
    {
        private readonly Dictionary<string, Restaurant> _restaurants = new(StringComparer.Ordinal);
        public Restaurant? CreatedRestaurant { get; private set; }
        public RestaurantBranch? CreatedBranch { get; private set; }

        public Task<IReadOnlyList<Restaurant>> ListRestaurantsAsync(NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Restaurant>>(_restaurants.Values.Where(item => item.OwnerUserId == ownerId).ToArray());
        public Task<Restaurant?> FindRestaurantAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(_restaurants.TryGetValue(restaurantId.Value, out var item) && item.OwnerUserId == ownerId ? item : null);
        public Task CreateRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken)
        {
            CreatedRestaurant = restaurant;
            _restaurants.Add(restaurant.Id.Value, restaurant);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<RestaurantBranch>> ListBranchesAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RestaurantBranch>>(CreatedBranch is not null && CreatedBranch.RestaurantId == restaurantId &&
                _restaurants.TryGetValue(restaurantId.Value, out var restaurant) && restaurant.OwnerUserId == ownerId ? [CreatedBranch] : []);
        public Task CreateBranchAsync(RestaurantBranch branch, NanoId ownerId, CancellationToken cancellationToken)
        {
            CreatedBranch = branch;
            return Task.CompletedTask;
        }
    }
}
