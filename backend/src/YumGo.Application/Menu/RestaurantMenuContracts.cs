using YumGo.Domain.Common;
using YumGo.Domain.Menu;

namespace YumGo.Application.Menu;

public interface IRestaurantMenuStore
{
    Task<MenuStoreResult<RestaurantMenuDto>> GetBranchMenuAsync(
        NanoId branchId, NanoId actorUserId, CancellationToken cancellationToken);

    Task<MenuStoreResult<MenuCategory>> CreateCategoryAsync(
        MenuCategory category, NanoId actorUserId, CancellationToken cancellationToken);

    Task<MenuStoreResult<MenuCategory>> UpdateCategoryAsync(
        NanoId categoryId, NanoId actorUserId, string name, int displayOrder, string status,
        DateTimeOffset now, CancellationToken cancellationToken);

    Task<MenuStoreResult<MenuItem>> CreateItemAsync(
        MenuItem item, NanoId actorUserId, CancellationToken cancellationToken);

    Task<MenuStoreResult<MenuItem>> UpdateItemAsync(
        NanoId itemId, NanoId actorUserId, NanoId categoryId, string name, string? description,
        decimal price, string status, DateTimeOffset now, CancellationToken cancellationToken);

    Task<MenuStoreResult<MenuItem>> SetItemAvailabilityAsync(
        NanoId itemId, NanoId actorUserId, bool available, DateTimeOffset now,
        CancellationToken cancellationToken);
}

public enum MenuStoreStatus { Created, Updated, Success, NotFound, Conflict }

public sealed record MenuStoreResult<T>(MenuStoreStatus Status, T? Value = default);

public enum RestaurantMenuStatus { Created, Updated, Success, ValidationFailed, NotFound, Conflict }

public sealed record RestaurantMenuResult(
    RestaurantMenuStatus Status,
    MenuCategoryDto? Category = null,
    RestaurantMenuDto? Menu = null,
    MenuItemDto? Item = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record MenuCategoryDto(
    string Id, string BranchId, string Name, int DisplayOrder, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record RestaurantMenuDto(string BranchId, IReadOnlyList<MenuCategoryWithItemsDto> Categories);

public sealed record MenuCategoryWithItemsDto(
    string Id, string BranchId, string Name, int DisplayOrder, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, IReadOnlyList<MenuItemDto> Items);

public sealed record MenuItemDto(
    string Id, string BranchId, string CategoryId, string Name, string? Description, decimal Price,
    string StockStatus, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
