using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using YumGo.Application.Restaurants;
using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;
using YumGo.Infrastructure.Persistence;
using YumGo.Infrastructure.Restaurants;

namespace YumGo.Infrastructure.Tests;

public sealed class RestaurantBranchStoreTests
{
    [Fact]
    public async Task Store_persists_owner_scope_and_audits_atomically_with_branch_creation()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_restaurants_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options;
        await using (var migrationContext = new ApplicationDbContext(options))
            await migrationContext.Database.MigrateAsync();

        var ownerId = NanoId.New();
        var otherUserId = NanoId.New();
        var now = DateTimeOffset.UtcNow;
        await using (var seedContext = new ApplicationDbContext(options))
        {
            await seedContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO users (id, email, password_hash, status, created_at, updated_at)
                VALUES ({ownerId.Value}, 'restaurant-owner@example.test', 'hash', 'active', {now}, {now}),
                       ({otherUserId.Value}, 'other-owner@example.test', 'hash', 'active', {now}, {now})
                """);
        }

        await using var context = new ApplicationDbContext(options);
        var store = new EfRestaurantBranchStore(context);
        var restaurant = Restaurant.Create(NanoId.New(), ownerId, "Pho House", "Local food", now);
        await store.CreateRestaurantAsync(restaurant, CancellationToken.None);

        var ownedRestaurants = await store.ListRestaurantsAsync(ownerId, CancellationToken.None);
        var hiddenRestaurants = await store.ListRestaurantsAsync(otherUserId, CancellationToken.None);
        Assert.Single(ownedRestaurants);
        Assert.Equal("pending", Assert.Single(ownedRestaurants).Status);
        Assert.Empty(hiddenRestaurants);
        Assert.Equal(1, await CountAsync(context, "restaurant.created"));

        var branch = RestaurantBranch.Create(NanoId.New(), restaurant.Id, "District 1", "+84912345678",
            "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10.7769m, 106.7009m,
            new TimeOnly(8, 0), new TimeOnly(22, 0), now);
        await store.CreateBranchAsync(branch, ownerId, CancellationToken.None);

        var branches = await store.ListBranchesAsync(restaurant.Id, ownerId, CancellationToken.None);
        Assert.Equal("draft", Assert.Single(branches).Status);
        Assert.Empty(await store.ListBranchesAsync(restaurant.Id, otherUserId, CancellationToken.None));
        Assert.Equal(1, await CountAsync(context, "restaurant.branch.created"));

        var foreignBranch = RestaurantBranch.Create(NanoId.New(), restaurant.Id, "District 2", "+84912345678",
            "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10m, 106m, null, null, now);
        await Assert.ThrowsAsync<RestaurantNotFoundException>(() =>
            store.CreateBranchAsync(foreignBranch, otherUserId, CancellationToken.None));
        Assert.Single(await store.ListBranchesAsync(restaurant.Id, ownerId, CancellationToken.None));
        Assert.Equal(1, await CountAsync(context, "restaurant.branch.created"));

        var duplicate = RestaurantBranch.Create(NanoId.New(), restaurant.Id, "district 1", "+84912345678",
            "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10m, 106m, null, null, now);
        await Assert.ThrowsAsync<RestaurantBranchNameConflictException>(() =>
            store.CreateBranchAsync(duplicate, ownerId, CancellationToken.None));
        Assert.Single(await store.ListBranchesAsync(restaurant.Id, ownerId, CancellationToken.None));
        Assert.Equal(1, await CountAsync(context, "restaurant.branch.created"));
    }

    private static Task<int> CountAsync(ApplicationDbContext context, string action) =>
        context.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM audit_logs WHERE action = {action}").SingleAsync();
}
