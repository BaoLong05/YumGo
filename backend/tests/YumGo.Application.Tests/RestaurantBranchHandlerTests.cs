using YumGo.Application.Restaurants;
using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Application.Tests;

public sealed class RestaurantBranchHandlerTests
{
    [Fact]
    public async Task Create_restaurant_trims_values_and_starts_pending()
    {
        var store = new FakeStore();
        var handler = new RestaurantBranchHandler(store, TimeProvider.System);

        var result = await handler.CreateRestaurantAsync(
            new CreateRestaurantCommand(NanoId.New().Value, "  Pho House  ", "  Family restaurant  "),
            CancellationToken.None);

        Assert.Equal(RestaurantBranchStatus.Created, result.Status);
        Assert.Equal("Pho House", result.Restaurant?.Name);
        Assert.Equal("Family restaurant", result.Restaurant?.Description);
        Assert.Equal("pending", result.Restaurant?.Status);
        Assert.NotNull(store.CreatedRestaurant);
    }

    [Fact]
    public async Task Create_branch_requires_owner_scope_and_starts_as_draft()
    {
        var ownerId = NanoId.New();
        var restaurant = Restaurant.Create(NanoId.New(), ownerId, "Pho House", null, DateTimeOffset.UtcNow);
        var store = new FakeStore { Restaurant = restaurant };
        var handler = new RestaurantBranchHandler(store, TimeProvider.System);

        var result = await handler.CreateBranchAsync(
            NanoId.New().Value,
            restaurant.Id.Value,
            new CreateRestaurantBranchCommand(
                "District 1", "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC",
                10.7769m, 106.7009m, null, null),
            CancellationToken.None);

        Assert.Equal(RestaurantBranchStatus.NotFound, result.Status);
        Assert.Null(store.CreatedBranch);

        result = await handler.CreateBranchAsync(
            ownerId.Value,
            restaurant.Id.Value,
            new CreateRestaurantBranchCommand(
                "District 1", "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC",
                10.7769m, 106.7009m, null, null),
            CancellationToken.None);

        Assert.Equal(RestaurantBranchStatus.Created, result.Status);
        Assert.Equal("draft", result.Branch?.Status);
        Assert.Equal(10.7769m, result.Branch?.Latitude);
        Assert.Equal(restaurant.Id.Value, result.Branch?.RestaurantId);
    }

    [Fact]
    public async Task Create_branch_rejects_partial_schedule_and_out_of_range_coordinates()
    {
        var ownerId = NanoId.New();
        var restaurant = Restaurant.Create(NanoId.New(), ownerId, "Pho House", null, DateTimeOffset.UtcNow);
        var store = new FakeStore { Restaurant = restaurant };
        var handler = new RestaurantBranchHandler(store, TimeProvider.System);

        var partialSchedule = await handler.CreateBranchAsync(
            ownerId.Value, restaurant.Id.Value,
            new CreateRestaurantBranchCommand(
                "District 1", "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC",
                10m, 106m, new TimeOnly(8, 0), null),
            CancellationToken.None);
        var invalidCoordinates = await handler.CreateBranchAsync(
            ownerId.Value, restaurant.Id.Value,
            new CreateRestaurantBranchCommand(
                "District 1", "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC",
                91m, 106m, null, null),
            CancellationToken.None);
        var overnightSchedule = await handler.CreateBranchAsync(
            ownerId.Value, restaurant.Id.Value,
            new CreateRestaurantBranchCommand(
                "District 2", "+84912345678", "12 Nguyen Hue", "Ben Nghe", "District 1", "HCMC",
                10m, 106m, new TimeOnly(22, 0), new TimeOnly(8, 0)),
            CancellationToken.None);

        Assert.Equal(RestaurantBranchStatus.ValidationFailed, partialSchedule.Status);
        Assert.Equal(RestaurantBranchStatus.ValidationFailed, invalidCoordinates.Status);
        Assert.Equal(RestaurantBranchStatus.ValidationFailed, overnightSchedule.Status);
        Assert.Equal(0, store.BranchCreateCalls);
    }

    private sealed class FakeStore : IRestaurantBranchStore
    {
        public Restaurant? Restaurant { get; init; }
        public Restaurant? CreatedRestaurant { get; private set; }
        public RestaurantBranch? CreatedBranch { get; private set; }
        public int BranchCreateCalls { get; private set; }

        public Task<IReadOnlyList<Restaurant>> ListRestaurantsAsync(NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Restaurant>>(Restaurant is not null && Restaurant.OwnerUserId == ownerId ? [Restaurant] : []);

        public Task<Restaurant?> FindRestaurantAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(Restaurant is not null && Restaurant.Id == restaurantId && Restaurant.OwnerUserId == ownerId ? Restaurant : null);

        public Task CreateRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken)
        {
            CreatedRestaurant = restaurant;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RestaurantBranch>> ListBranchesAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RestaurantBranch>>([]);

        public Task CreateBranchAsync(RestaurantBranch branch, NanoId ownerId, CancellationToken cancellationToken)
        {
            BranchCreateCalls++;
            CreatedBranch = branch;
            return Task.CompletedTask;
        }
    }
}
