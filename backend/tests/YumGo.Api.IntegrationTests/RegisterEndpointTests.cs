using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YumGo.Application.Identity;
using YumGo.Domain.Identity;

namespace YumGo.Api.IntegrationTests;

public sealed class RegisterEndpointTests
{
    [Fact]
    public async Task Valid_registration_returns_public_profile_without_credentials_or_tokens()
    {
        var store = new RecordingStore();
        using var factory = CreateFactory(store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = " Long.User@Example.com ",
            phone = "+84 912-345-678",
            password = "Long!Pass1",
            fullName = "  Long Tran  ",
            id = "client-controlled-id",
            status = "disabled",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal("long.user@example.com", data.GetProperty("email").GetString());
        Assert.Equal("Long Tran", data.GetProperty("fullName").GetString());
        Assert.DoesNotContain("password", body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(store.User);
        Assert.Equal(UserStatus.Active, store.User.Status);
        Assert.NotEqual("client-controlled-id", store.User.Id.Value);
        Assert.Equal("hashed-password", store.User.PasswordHash.Value);
        Assert.Equal("+84912345678", store.User.PhoneNumber?.Value);
    }

    [Fact]
    public async Task Invalid_registration_returns_422_field_errors_without_writing()
    {
        var store = new RecordingStore();
        using var factory = CreateFactory(store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "invalid",
            phone = "not-a-phone",
            password = "abc",
            fullName = "A"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("phone", out _));
        Assert.True(errors.TryGetProperty("password", out _));
        Assert.True(errors.TryGetProperty("fullName", out _));
        Assert.Null(store.User);
    }

    [Fact]
    public async Task Duplicate_account_returns_generic_409_without_revealing_which_field_exists()
    {
        var store = new RecordingStore { Result = CustomerRegistrationStoreStatus.DuplicateEmail };
        using var factory = CreateFactory(store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "existing@example.com",
            password = "Long!Pass1",
            fullName = "Long Tran"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("An account with these details already exists.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("existing@example.com", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registration_is_rate_limited_per_client_ip()
    {
        var store = new RecordingStore();
        using var factory = CreateFactory(store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        for (var request = 0; request < 5; request++)
        {
            using var acceptedResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email = $"rate-limit-{request}@example.com",
                password = "Long!Pass1",
                fullName = "Long Tran"
            });
            Assert.Equal(HttpStatusCode.Created, acceptedResponse.StatusCode);
        }

        using var limitedResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "rate-limit-final@example.com",
            password = "Long!Pass1",
            fullName = "Long Tran"
        });
        Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(RecordingStore store) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICustomerRegistrationStore>();
                services.AddSingleton<ICustomerRegistrationStore>(store);
                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, FakePasswordHashingService>();
            }));

    private sealed class RecordingStore : ICustomerRegistrationStore
    {
        public CustomerRegistrationStoreStatus Result { get; init; } = CustomerRegistrationStoreStatus.Created;

        public User? User { get; private set; }

        public CustomerProfile? Profile { get; private set; }

        public Task<CustomerRegistrationStoreStatus> CreateAsync(
            User user,
            CustomerProfile profile,
            CancellationToken cancellationToken)
        {
            if (Result == CustomerRegistrationStoreStatus.Created)
            {
                User = user;
                Profile = profile;
            }

            return Task.FromResult(Result);
        }
    }

    private sealed class FakePasswordHashingService : IPasswordHashingService
    {
        public PasswordHash Hash(string password) => PasswordHash.FromEncodedHash("hashed-password");

        public bool Verify(string password, PasswordHash passwordHash) => false;
    }
}
