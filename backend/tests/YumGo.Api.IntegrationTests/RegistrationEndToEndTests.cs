using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;
using YumGo.Infrastructure.Security;

namespace YumGo.Api.IntegrationTests;

public sealed class RegistrationEndToEndTests
{
    [Fact]
    public async Task Registration_persists_user_profile_and_customer_role_in_postgresql()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_api_registration_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            TestJwtConfiguration.Apply(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = postgres.GetConnectionString()
                }));
            builder.ConfigureLogging(logging =>
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None));
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "end-to-end@example.com",
            phone = "+84 912-345-678",
            password = "Long!Pass1",
            fullName = "Long Tran"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var responseBody = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var customerId = responseBody.RootElement.GetProperty("data").GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(customerId));

        Assert.Equal(1, await CountAsync(dbContext, "users"));
        Assert.Equal(1, await CountAsync(dbContext, "customer_profiles"));
        Assert.Equal(1, await CountAsync(dbContext, "user_roles"));
        Assert.Equal(1, await dbContext.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM roles WHERE code = 'customer'")
            .SingleAsync());
        Assert.Equal("+84912345678", await dbContext.Database
            .SqlQueryRaw<string>("SELECT phone AS \"Value\" FROM users WHERE email = 'end-to-end@example.com'")
            .SingleAsync());

        var storedHash = await dbContext.Database
            .SqlQueryRaw<string>("SELECT password_hash AS \"Value\" FROM users WHERE email = 'end-to-end@example.com'")
            .SingleAsync();
        Assert.NotEqual("Long!Pass1", storedHash);
        Assert.True(new AspNetPasswordHashingService().Verify(
            "Long!Pass1",
            PasswordHash.FromEncodedHash(storedHash)));
    }

    private static Task<int> CountAsync(ApplicationDbContext context, string tableName)
    {
        var query = tableName switch
        {
            "users" => "SELECT count(*)::int AS \"Value\" FROM users",
            "customer_profiles" => "SELECT count(*)::int AS \"Value\" FROM customer_profiles",
            "user_roles" => "SELECT count(*)::int AS \"Value\" FROM user_roles",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Unexpected table name.")
        };

        return context.Database.SqlQueryRaw<int>(query).SingleAsync();
    }
}
