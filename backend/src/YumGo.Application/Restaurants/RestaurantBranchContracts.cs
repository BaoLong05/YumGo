using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Application.Restaurants;

public interface IRestaurantBranchStore
{
    Task<IReadOnlyList<Restaurant>> ListRestaurantsAsync(NanoId ownerId, CancellationToken cancellationToken);
    Task<Restaurant?> FindRestaurantAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken);
    Task CreateRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken);
    Task<IReadOnlyList<RestaurantBranch>> ListBranchesAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken);
    Task CreateBranchAsync(RestaurantBranch branch, NanoId ownerId, CancellationToken cancellationToken);
}

public sealed record CreateRestaurantCommand(string? OwnerUserId, string? Name, string? Description);
public sealed record CreateRestaurantBranchCommand(
    string? Name, string? Phone, string? AddressLine, string? Ward, string? District, string? City,
    decimal? Latitude, decimal? Longitude, TimeOnly? OpensAt, TimeOnly? ClosesAt);

public sealed record RestaurantDto(string Id, string Name, string? Description, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record RestaurantBranchDto(string Id, string RestaurantId, string Name, string Phone,
    string AddressLine, string Ward, string District, string City, decimal Latitude, decimal Longitude,
    string Status, TimeOnly? OpensAt, TimeOnly? ClosesAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public enum RestaurantBranchStatus { Success, Created, ValidationFailed, NotFound, DuplicateName }
public sealed record RestaurantBranchResult(RestaurantBranchStatus Status,
    RestaurantDto? Restaurant = null, RestaurantBranchDto? Branch = null,
    IReadOnlyList<RestaurantDto>? Restaurants = null, IReadOnlyList<RestaurantBranchDto>? Branches = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed class RestaurantBranchNameConflictException() : Exception("A branch with this name already exists.");
public sealed class RestaurantNotFoundException() : Exception("The restaurant was not found for this owner.");
