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
using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;

namespace YumGo.Api.IntegrationTests;

public sealed class CustomerAddressEndpointsTests
{
    [Fact]
    public async Task Address_endpoints_require_auth_geocode_server_side_and_enforce_ownership()
    {
        var signingKey = TestJwtConfiguration.CreateSigningKey();
        var store = new FakeAddressStore();
        var geocoder = new FakeGeocoder();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder, signingKey);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused" }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationStore>();
                services.AddSingleton<IAuthenticationStore, FakeAuthenticationStore>();
                services.RemoveAll<ICustomerAddressStore>();
                services.AddSingleton<ICustomerAddressStore>(store);
                services.RemoveAll<IAddressReverseGeocoder>();
                services.AddSingleton<IAddressReverseGeocoder>(geocoder);
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        using var anonymousResponse = await client.GetAsync("/api/v1/me/addresses");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var customerId = NanoId.New().Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(customerId, signingKey));
        using var rejectedCreate = await client.PostAsJsonAsync("/api/v1/me/addresses", new
        {
            label = "Home",
            recipientName = "Long Tran",
            recipientPhone = "+84912345678",
            latitude = 10.7769,
            longitude = 106.7009,
            isDefault = true,
            addressLine = "FORGED FREE-FORM ADDRESS"
        });
        Assert.Equal(HttpStatusCode.BadRequest, rejectedCreate.StatusCode);
        Assert.Equal(0, geocoder.Calls);

        using var createResponse = await client.PostAsJsonAsync("/api/v1/me/addresses", new
        {
            label = "Home",
            recipientName = "Long Tran",
            recipientPhone = "+84912345678",
            latitude = 10.7769,
            longitude = 106.7009,
            isDefault = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = await System.Text.Json.JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var data = createdJson.RootElement.GetProperty("data");
        var addressId = data.GetProperty("id").GetString()!;
        Assert.Equal("12 Nguyen Hue", data.GetProperty("addressLine").GetString());
        Assert.True(data.GetProperty("isDefault").GetBoolean());
        Assert.Equal(1, geocoder.Calls);

        geocoder.Exceeded = true;
        using var quotaExceeded = await client.PostAsJsonAsync("/api/v1/me/addresses", new
        {
            label = "Work",
            recipientName = "Long Tran",
            recipientPhone = "+84912345678",
            latitude = 10.7769,
            longitude = 106.7009
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, quotaExceeded.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(NanoId.New().Value, signingKey));
        using var foreignUpdate = await client.PatchAsJsonAsync($"/api/v1/me/addresses/{addressId}", new { label = "Stolen" });
        Assert.Equal(HttpStatusCode.NotFound, foreignUpdate.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(customerId, signingKey));
        using var setDefault = await client.PostAsync($"/api/v1/me/addresses/{addressId}/default", null);
        Assert.Equal(HttpStatusCode.NoContent, setDefault.StatusCode);
        using var delete = await client.DeleteAsync($"/api/v1/me/addresses/{addressId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var list = await client.GetAsync("/api/v1/me/addresses");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var listJson = await System.Text.Json.JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
        Assert.Empty(listJson.RootElement.GetProperty("data").EnumerateArray());
    }

    private static string CreateToken(string userId, string signingKey)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "YumGo.Tests",
            audience: "YumGo.Tests.Client",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("sid", NanoId.New().Value)
            ],
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class FakeGeocoder : IAddressReverseGeocoder
    {
        public int Calls { get; private set; }
        public bool Exceeded { get; set; }

        public Task<ReverseGeocodedAddress?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
        {
            Calls++;
            if (Exceeded) throw new GeocodingQuotaExceededException();
            return Task.FromResult<ReverseGeocodedAddress?>(
                new("12 Nguyen Hue", "Ben Nghe", "District 1", "Ho Chi Minh City"));
        }
    }

    private sealed class FakeAddressStore : ICustomerAddressStore
    {
        private readonly Dictionary<string, CustomerAddress> _addresses = new(StringComparer.Ordinal);

        public Task<IReadOnlyList<CustomerAddress>> ListAsync(NanoId userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CustomerAddress>>(_addresses.Values
                .Where(address => address.UserId == userId)
                .OrderByDescending(address => address.IsDefault)
                .ToArray());

        public Task<CustomerAddress?> FindAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) =>
            Task.FromResult(_addresses.TryGetValue(addressId.Value, out var address) && address.UserId == userId ? address : null);

        public Task CreateAsync(CustomerAddress address, CancellationToken cancellationToken)
        {
            if (address.IsDefault)
                foreach (var existing in _addresses.Values.Where(existing => existing.UserId == address.UserId))
                    existing.SetDefault(false, address.UpdatedAt);
            _addresses.Add(address.Id.Value, address);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(CustomerAddress address, bool? isDefaultOverride, CancellationToken cancellationToken)
        {
            if (!_addresses.TryGetValue(address.Id.Value, out var existing) || existing.UserId != address.UserId) return Task.FromResult(false);
            if (address.IsDefault)
                foreach (var other in _addresses.Values.Where(other => other.UserId == address.UserId && other.Id != address.Id))
                    other.SetDefault(false, address.UpdatedAt);
            _addresses[address.Id.Value] = address;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken) =>
            Task.FromResult(_addresses.TryGetValue(addressId.Value, out var address)
                && address.UserId == userId && _addresses.Remove(addressId.Value));

        public Task<bool> SetDefaultAsync(NanoId userId, NanoId addressId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (!_addresses.TryGetValue(addressId.Value, out var target) || target.UserId != userId) return Task.FromResult(false);
            foreach (var address in _addresses.Values.Where(address => address.UserId == userId))
                address.SetDefault(address.Id == addressId, now);
            return Task.FromResult(true);
        }
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
}
