using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Api.IntegrationTests;

internal sealed class AuthApiTestHost : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly AsyncServiceScope _scope;

    private AuthApiTestHost(
        PostgreSqlContainer postgres,
        WebApplicationFactory<Program> factory,
        AsyncServiceScope scope,
        HttpClient client,
        string signingKey,
        ApplicationDbContext dbContext)
    {
        _postgres = postgres;
        _factory = factory;
        _scope = scope;
        Client = client;
        SigningKey = signingKey;
        DbContext = dbContext;
    }

    public HttpClient Client { get; }

    public string SigningKey { get; }

    public ApplicationDbContext DbContext { get; }

    public static async Task<AuthApiTestHost> StartAsync()
    {
        var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_api_auth_session_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();

        var signingKey = TestJwtConfiguration.CreateSigningKey();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder, signingKey);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString()
                }));
        });

        var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return new AuthApiTestHost(postgres, factory, scope, client, signingKey, dbContext);
    }

    public async Task<string> RegisterCustomerAsync(string email, string password = "Long!Pass1")
    {
        using var response = await Client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            fullName = "Auth Session"
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").GetProperty("id").GetString()!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _scope.DisposeAsync();
        _factory.Dispose();
        await _postgres.DisposeAsync();
    }
}
