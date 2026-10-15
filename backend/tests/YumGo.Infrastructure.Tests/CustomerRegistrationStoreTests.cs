using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;
using YumGo.Infrastructure.Registration;

namespace YumGo.Infrastructure.Tests;

public sealed class CustomerRegistrationStoreTests
{
    [Fact]
    public async Task Store_creates_profile_and_customer_role_and_returns_database_uniqueness_conflicts()
    {
        await using var postgres = await CreatePostgresAsync();
        var options = CreateOptions(postgres);
        await MigrateAsync(options);

        var firstUser = CreateUser("first@example.com", "+84912345678");
        var firstProfile = CustomerProfile.Register(firstUser.Id, "First Customer");
        await using (var context = new ApplicationDbContext(options))
        {
            var status = await new EfCustomerRegistrationStore(context)
                .CreateAsync(firstUser, firstProfile, CancellationToken.None);

            Assert.Equal(YumGo.Application.Identity.CustomerRegistrationStoreStatus.Created, status);
            Assert.Equal(1, await CountAsync(context, "users"));
            Assert.Equal(1, await CountAsync(context, "customer_profiles"));
            Assert.Equal(1, await CountAsync(context, "user_roles"));
        }

        var duplicateEmailUser = CreateUser("first@example.com", null);
        var duplicateEmailProfile = CustomerProfile.Register(duplicateEmailUser.Id, "Duplicate Email");
        await using (var context = new ApplicationDbContext(options))
        {
            var status = await new EfCustomerRegistrationStore(context)
                .CreateAsync(duplicateEmailUser, duplicateEmailProfile, CancellationToken.None);
            Assert.Equal(YumGo.Application.Identity.CustomerRegistrationStoreStatus.DuplicateEmail, status);
            Assert.Equal(1, await CountAsync(context, "users"));
            Assert.Equal(1, await CountAsync(context, "customer_profiles"));
        }

        var duplicatePhoneUser = CreateUser("second@example.com", "+84912345678");
        var duplicatePhoneProfile = CustomerProfile.Register(duplicatePhoneUser.Id, "Duplicate Phone");
        await using (var context = new ApplicationDbContext(options))
        {
            var status = await new EfCustomerRegistrationStore(context)
                .CreateAsync(duplicatePhoneUser, duplicatePhoneProfile, CancellationToken.None);
            Assert.Equal(YumGo.Application.Identity.CustomerRegistrationStoreStatus.DuplicatePhone, status);
            Assert.Equal(1, await CountAsync(context, "users"));
            Assert.Equal(1, await CountAsync(context, "customer_profiles"));
        }
    }

    [Fact]
    public async Task Store_rolls_back_user_and_profile_when_customer_role_is_unavailable()
    {
        await using var postgres = await CreatePostgresAsync();
        var options = CreateOptions(postgres);
        await MigrateAsync(options);

        await using var context = new ApplicationDbContext(options);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM roles WHERE code = 'customer'");
        var user = CreateUser("atomic@example.com", null);
        var profile = CustomerProfile.Register(user.Id, "Atomic Test");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EfCustomerRegistrationStore(context).CreateAsync(user, profile, CancellationToken.None));

        Assert.Equal(0, await CountAsync(context, "users"));
        Assert.Equal(0, await CountAsync(context, "customer_profiles"));
        Assert.Equal(0, await CountAsync(context, "user_roles"));
    }

    private static async Task<PostgreSqlContainer> CreatePostgresAsync()
    {
        var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_registration_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();
        return postgres;
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(PostgreSqlContainer postgres) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

    private static async Task MigrateAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }

    private static User CreateUser(string email, string? phone) =>
        User.Register(
            Email.Create(email),
            PasswordHash.FromEncodedHash("not-a-real-password-hash"),
            phone is null ? null : PhoneNumber.Create(phone));

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
