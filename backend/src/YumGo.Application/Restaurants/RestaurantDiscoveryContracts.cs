using YumGo.Domain.Common;

namespace YumGo.Application.Restaurants;

public sealed record RestaurantDiscoveryQuery(
    string? Search, string? City, string? District, bool? OpenNow, string? CategoryId,
    DateTimeOffset? CursorCreatedAt, string? CursorId, int Limit);

public sealed record RestaurantDiscoveryPage(IReadOnlyList<RestaurantDiscoverySummary> Items, string? NextCursor, bool HasMore);

public sealed record RestaurantDiscoverySummary(
    string Id, string Name, string? Description, string BranchId, string BranchName,
    string District, string City, decimal Latitude, decimal Longitude, string Status, TimeOnly? OpensAt, TimeOnly? ClosesAt);

public sealed record PublicRestaurant(string Id, string Name, string? Description);
public sealed record PublicBranch(string Id, string RestaurantId, string RestaurantName, string Name,
    string Phone, string AddressLine, string Ward, string District, string City, decimal Latitude,
    decimal Longitude, string Status, TimeOnly? OpensAt, TimeOnly? ClosesAt);

public sealed record PublicMenu(IReadOnlyList<PublicMenuCategory> Categories);
public sealed record PublicMenuCategory(string Id, string Name, int DisplayOrder, IReadOnlyList<PublicMenuItem> Items);
public sealed record PublicMenuItem(string Id, string Name, string? Description, decimal Price, string StockStatus);

public interface IRestaurantDiscoveryStore
{
    Task<RestaurantDiscoveryPage> SearchAsync(RestaurantDiscoveryQuery query, CancellationToken cancellationToken);
    Task<PublicRestaurant?> GetRestaurantAsync(NanoId restaurantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicBranch>> GetBranchesAsync(NanoId restaurantId, CancellationToken cancellationToken);
    Task<PublicBranch?> GetBranchAsync(NanoId branchId, CancellationToken cancellationToken);
    Task<PublicMenu?> GetPublicMenuAsync(NanoId branchId, CancellationToken cancellationToken);
}

public enum RestaurantDiscoveryStatus { Success, ValidationFailed, NotFound }
public sealed record RestaurantDiscoveryResult(
    RestaurantDiscoveryStatus Status,
    RestaurantDiscoveryPage? Page = null,
    PublicRestaurant? Restaurant = null,
    IReadOnlyList<PublicBranch>? Branches = null,
    PublicBranch? Branch = null,
    PublicMenu? Menu = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
