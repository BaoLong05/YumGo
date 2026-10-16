using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Application.Restaurants;

public sealed class RestaurantBranchHandler(
    IRestaurantBranchStore store,
    TimeProvider timeProvider)
{
    public async Task<RestaurantBranchResult> ListRestaurantsAsync(string? ownerUserId, CancellationToken cancellationToken)
    {
        if (!TryNanoId(ownerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        var restaurants = await store.ListRestaurantsAsync(ownerId!, cancellationToken);
        return new(RestaurantBranchStatus.Success, Restaurants: restaurants.Select(ToDto).ToArray());
    }

    public async Task<RestaurantBranchResult> CreateRestaurantAsync(CreateRestaurantCommand command, CancellationToken cancellationToken)
    {
        if (!TryNanoId(command.OwnerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        var errors = new Dictionary<string, string[]>();
        AddTextError(errors, "name", command.Name, 2, 160);
        if (command.Description?.Trim().Length > 2000) errors["description"] = ["Description cannot exceed 2000 characters."];
        if (errors.Count > 0) return new(RestaurantBranchStatus.ValidationFailed, Errors: errors);

        var now = timeProvider.GetUtcNow();
        var restaurant = Restaurant.Create(NanoId.New(), ownerId!, command.Name!, command.Description, now);
        await store.CreateRestaurantAsync(restaurant, cancellationToken);
        return new(RestaurantBranchStatus.Created, Restaurant: ToDto(restaurant));
    }

    public async Task<RestaurantBranchResult> ListBranchesAsync(
        string? ownerUserId, string? restaurantId, CancellationToken cancellationToken)
    {
        if (!TryNanoId(ownerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        if (!TryNanoId(restaurantId, out var parsedRestaurantId)) return Invalid("restaurantId", "A valid restaurant ID is required.");
        var restaurant = await store.FindRestaurantAsync(parsedRestaurantId!, ownerId!, cancellationToken);
        if (restaurant is null) return new(RestaurantBranchStatus.NotFound);
        var branches = await store.ListBranchesAsync(parsedRestaurantId!, ownerId!, cancellationToken);
        return new(RestaurantBranchStatus.Success, Branches: branches.Select(ToDto).ToArray());
    }

    public async Task<RestaurantBranchResult> CreateBranchAsync(
        string? ownerUserId, string? restaurantId, CreateRestaurantBranchCommand command, CancellationToken cancellationToken)
    {
        if (!TryNanoId(ownerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        if (!TryNanoId(restaurantId, out var parsedRestaurantId)) return Invalid("restaurantId", "A valid restaurant ID is required.");
        var errors = new Dictionary<string, string[]>();
        AddTextError(errors, "name", command.Name, 2, 160);
        AddTextError(errors, "phone", command.Phone, 8, 20);
        AddTextError(errors, "addressLine", command.AddressLine, 5, 255);
        AddTextError(errors, "ward", command.Ward, 1, 100);
        AddTextError(errors, "district", command.District, 1, 100);
        AddTextError(errors, "city", command.City, 1, 100);
        if (command.Latitude is null or < -90 or > 90) errors["latitude"] = ["Latitude is required and must be between -90 and 90."];
        if (command.Longitude is null or < -180 or > 180) errors["longitude"] = ["Longitude is required and must be between -180 and 180."];
        if (command.OpensAt.HasValue != command.ClosesAt.HasValue)
            errors["schedule"] = ["Opening and closing times must be provided together."];
        else if (command.OpensAt.HasValue && command.OpensAt.Value >= command.ClosesAt.GetValueOrDefault())
            errors["schedule"] = ["Overnight schedules are not supported; opening time must be before closing time."];
        if (errors.Count > 0) return new(RestaurantBranchStatus.ValidationFailed, Errors: errors);

        var restaurant = await store.FindRestaurantAsync(parsedRestaurantId!, ownerId!, cancellationToken);
        if (restaurant is null) return new(RestaurantBranchStatus.NotFound);
        RestaurantBranch branch;
        try
        {
            branch = RestaurantBranch.Create(NanoId.New(), restaurant.Id, command.Name!, command.Phone!, command.AddressLine!,
                command.Ward!, command.District!, command.City!, command.Latitude.GetValueOrDefault(), command.Longitude.GetValueOrDefault(),
                command.OpensAt, command.ClosesAt, timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            return new(RestaurantBranchStatus.ValidationFailed, Errors: new Dictionary<string, string[]> {
                [exception.ParamName ?? "request"] = [exception.Message]
            });
        }

        try
        {
            await store.CreateBranchAsync(branch, ownerId!, cancellationToken);
        }
        catch (RestaurantBranchNameConflictException)
        {
            return new(RestaurantBranchStatus.DuplicateName);
        }
        catch (RestaurantNotFoundException)
        {
            return new(RestaurantBranchStatus.NotFound);
        }
        return new(RestaurantBranchStatus.Created, Branch: ToDto(branch));
    }

    private static bool TryNanoId(string? value, out NanoId? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { id = NanoId.Create(value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static void AddTextError(IDictionary<string, string[]> errors, string field, string? value, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < min || value.Trim().Length > max)
            errors[field] = [$"Must contain between {min} and {max} characters."];
    }

    private static RestaurantBranchResult Invalid(string field, string message) =>
        new(RestaurantBranchStatus.ValidationFailed, Errors: new Dictionary<string, string[]> { [field] = [message] });

    private static RestaurantDto ToDto(Restaurant restaurant) => new(restaurant.Id.Value, restaurant.Name,
        restaurant.Description, restaurant.Status, restaurant.CreatedAt, restaurant.UpdatedAt);

    private static RestaurantBranchDto ToDto(RestaurantBranch branch) => new(branch.Id.Value, branch.RestaurantId.Value,
        branch.Name, branch.Phone.Value, branch.AddressLine, branch.Ward, branch.District, branch.City,
        branch.Latitude, branch.Longitude, branch.Status, branch.OpensAt, branch.ClosesAt, branch.CreatedAt, branch.UpdatedAt);
}
