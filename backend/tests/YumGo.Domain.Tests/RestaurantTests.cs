using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Domain.Tests;

public sealed class RestaurantTests
{
    [Fact]
    public void Restaurant_creation_starts_pending_and_trims_name()
    {
        var now = DateTimeOffset.UtcNow;
        var restaurant = Restaurant.Create(NanoId.New(), NanoId.New(), "  Pho House ", null, now);

        Assert.Equal("Pho House", restaurant.Name);
        Assert.Equal("pending", restaurant.Status);
    }

    [Fact]
    public void Restoring_restaurant_preserves_persisted_status_and_timestamps()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-2);
        var updatedAt = DateTimeOffset.UtcNow;
        var restaurant = Restaurant.Restore(NanoId.New(), NanoId.New(), "Pho House", null, "active", createdAt, updatedAt);

        Assert.Equal("active", restaurant.Status);
        Assert.Equal(createdAt, restaurant.CreatedAt);
        Assert.Equal(updatedAt, restaurant.UpdatedAt);
    }

    [Fact]
    public void Branch_creation_requires_both_schedule_times_and_valid_coordinates()
    {
        Assert.Throws<ArgumentException>(() => RestaurantBranch.Create(NanoId.New(), NanoId.New(), "District 1",
            "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10m, 106m,
            new TimeOnly(8, 0), null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => RestaurantBranch.Create(NanoId.New(), NanoId.New(), "District 1",
            "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 91m, 106m,
            null, null, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => RestaurantBranch.Create(NanoId.New(), NanoId.New(), "District 1",
            "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10m, 106m,
            new TimeOnly(22, 0), new TimeOnly(8, 0), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Restoring_branch_preserves_persisted_status_and_updated_time()
    {
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;
        var branch = RestaurantBranch.Restore(NanoId.New(), NanoId.New(), "District 1", "+84912345678",
            "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC", 10m, 106m, "open", null, null,
            createdAt, updatedAt);

        Assert.Equal("open", branch.Status);
        Assert.Equal(updatedAt, branch.UpdatedAt);
    }
}
