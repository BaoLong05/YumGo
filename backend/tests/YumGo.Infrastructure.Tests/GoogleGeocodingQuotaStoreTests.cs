using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using YumGo.Infrastructure.Addresses;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Tests;

public sealed class GoogleGeocodingQuotaStoreTests
{
    [Fact]
    public async Task Monthly_quota_reservations_are_atomic_and_capped_across_connections()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_geocoding_quota_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using (var migrationContext = new ApplicationDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 10, 16, 3, 15, 0, TimeSpan.Zero));
        var reservations = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var context = new ApplicationDbContext(options);
            var quota = new PostgresGoogleGeocodingQuotaStore(context, timeProvider);
            return await quota.TryReserveAsync(10, CancellationToken.None);
        }));

        Assert.Equal(10, reservations.Count(reserved => reserved));
        await using var verificationContext = new ApplicationDbContext(options);
        var count = await verificationContext.Database
            .SqlQueryRaw<int>("SELECT request_count AS \"Value\" FROM google_geocoding_usage WHERE month_start = DATE '2026-10-01'")
            .SingleAsync();
        Assert.Equal(10, count);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
