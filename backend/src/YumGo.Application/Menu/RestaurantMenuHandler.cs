using YumGo.Domain.Common;
using YumGo.Domain.Menu;

namespace YumGo.Application.Menu;

public sealed class RestaurantMenuHandler(IRestaurantMenuStore store, TimeProvider timeProvider)
{
    public async Task<RestaurantMenuResult> GetBranchMenuAsync(
        string? actorUserId, string? branchId, CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(branchId, out var parsedBranchId)) return Invalid("branchId", "A valid branch ID is required.");
        var result = await store.GetBranchMenuAsync(parsedBranchId!, actorId!, cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Success when result.Value is not null => new(RestaurantMenuStatus.Success, Menu: result.Value),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            _ => throw new InvalidOperationException("The menu store returned an unsupported read result.")
        };
    }

    public async Task<RestaurantMenuResult> CreateCategoryAsync(
        string? actorUserId, string? branchId, string? name, int? displayOrder, CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(branchId, out var parsedBranchId)) return Invalid("branchId", "A valid branch ID is required.");
        if (displayOrder is null) return Invalid("displayOrder", "Display order is required.");
        MenuCategory category;
        try { category = MenuCategory.Create(NanoId.New(), parsedBranchId!, name ?? "", displayOrder.Value, timeProvider.GetUtcNow()); }
        catch (ArgumentException exception) { return Invalid(exception.ParamName == "displayOrder" ? "displayOrder" : "name", exception.Message); }

        var result = await store.CreateCategoryAsync(category, actorId!, cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Created when result.Value is not null => new(RestaurantMenuStatus.Created, ToDto(result.Value)),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            MenuStoreStatus.Conflict => new(RestaurantMenuStatus.Conflict),
            _ => throw new InvalidOperationException("The menu store returned an unsupported create result.")
        };
    }

    public async Task<RestaurantMenuResult> UpdateCategoryAsync(
        string? actorUserId, string? categoryId, string? name, int? displayOrder, string? status,
        CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(categoryId, out var parsedCategoryId)) return Invalid("categoryId", "A valid category ID is required.");
        if (displayOrder is null) return Invalid("displayOrder", "Display order is required.");
        var normalizedStatus = status?.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("active" or "inactive")) return Invalid("status", "Status must be 'active' or 'inactive'.");
        string normalizedName;
        try { normalizedName = MenuCategory.Create(NanoId.New(), NanoId.New(), name ?? "", displayOrder.Value, timeProvider.GetUtcNow()).Name; }
        catch (ArgumentException exception) { return Invalid(exception.ParamName == "displayOrder" ? "displayOrder" : "name", exception.Message); }

        var result = await store.UpdateCategoryAsync(parsedCategoryId!, actorId!, normalizedName,
            displayOrder.Value, normalizedStatus, timeProvider.GetUtcNow(), cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Updated when result.Value is not null => new(RestaurantMenuStatus.Updated, ToDto(result.Value)),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            MenuStoreStatus.Conflict => new(RestaurantMenuStatus.Conflict),
            _ => throw new InvalidOperationException("The menu store returned an unsupported update result.")
        };
    }

    public async Task<RestaurantMenuResult> CreateItemAsync(
        string? actorUserId, string? branchId, string? categoryId, string? name, string? description,
        decimal? price, CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(branchId, out var parsedBranchId)) return Invalid("branchId", "A valid branch ID is required.");
        if (!TryNanoId(categoryId, out var parsedCategoryId)) return Invalid("categoryId", "A valid category ID is required.");
        if (price is null) return Invalid("price", "Price is required.");
        MenuItem item;
        try { item = MenuItem.Create(NanoId.New(), parsedBranchId!, parsedCategoryId!, name ?? "", description, price.Value, timeProvider.GetUtcNow()); }
        catch (ArgumentException exception) { return Invalid(ItemField(exception), exception.Message); }
        var result = await store.CreateItemAsync(item, actorId!, cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Created when result.Value is not null => new(RestaurantMenuStatus.Created, Item: ToDto(result.Value)),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            MenuStoreStatus.Conflict => new(RestaurantMenuStatus.Conflict),
            _ => throw new InvalidOperationException("The menu store returned an unsupported item create result.")
        };
    }

    public async Task<RestaurantMenuResult> UpdateItemAsync(
        string? actorUserId, string? itemId, string? categoryId, string? name, string? description,
        decimal? price, string? status, CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(itemId, out var parsedItemId)) return Invalid("itemId", "A valid menu item ID is required.");
        if (!TryNanoId(categoryId, out var parsedCategoryId)) return Invalid("categoryId", "A valid category ID is required.");
        if (price is null) return Invalid("price", "Price is required.");
        var normalizedStatus = status?.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("active" or "inactive")) return Invalid("status", "Status must be 'active' or 'inactive'.");
        MenuItem validated;
        try { validated = MenuItem.Create(NanoId.New(), NanoId.New(), parsedCategoryId!, name ?? "", description, price.Value, timeProvider.GetUtcNow()); }
        catch (ArgumentException exception) { return Invalid(ItemField(exception), exception.Message); }
        var result = await store.UpdateItemAsync(parsedItemId!, actorId!, parsedCategoryId!, validated.Name,
            validated.Description, validated.Price, normalizedStatus, timeProvider.GetUtcNow(), cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Updated when result.Value is not null => new(RestaurantMenuStatus.Updated, Item: ToDto(result.Value)),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            MenuStoreStatus.Conflict => new(RestaurantMenuStatus.Conflict),
            _ => throw new InvalidOperationException("The menu store returned an unsupported item update result.")
        };
    }

    public async Task<RestaurantMenuResult> SetItemAvailabilityAsync(
        string? actorUserId, string? itemId, bool available, CancellationToken cancellationToken)
    {
        if (!TryNanoId(actorUserId, out var actorId)) return Invalid("actorUserId", "A valid user ID is required.");
        if (!TryNanoId(itemId, out var parsedItemId)) return Invalid("itemId", "A valid menu item ID is required.");
        var result = await store.SetItemAvailabilityAsync(parsedItemId!, actorId!, available, timeProvider.GetUtcNow(), cancellationToken);
        return result.Status switch
        {
            MenuStoreStatus.Updated when result.Value is not null => new(RestaurantMenuStatus.Updated, Item: ToDto(result.Value)),
            MenuStoreStatus.NotFound => new(RestaurantMenuStatus.NotFound),
            _ => throw new InvalidOperationException("The menu store returned an unsupported availability result.")
        };
    }

    private static string ItemField(ArgumentException exception) => exception.ParamName switch
    {
        "price" => "price", "description" => "description", _ => "name"
    };

    private static bool TryNanoId(string? value, out NanoId? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { id = NanoId.Create(value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static RestaurantMenuResult Invalid(string field, string message) =>
        new(RestaurantMenuStatus.ValidationFailed, Errors: new Dictionary<string, string[]> { [field] = [message] });

    private static MenuCategoryDto ToDto(MenuCategory category) => new(
        category.Id.Value, category.BranchId.Value, category.Name, category.DisplayOrder,
        category.Status, category.CreatedAt, category.UpdatedAt);

    private static MenuItemDto ToDto(MenuItem item) => new(
        item.Id.Value, item.BranchId.Value, item.CategoryId.Value, item.Name, item.Description, item.Price,
        item.StockStatus, item.Status, item.CreatedAt, item.UpdatedAt);
}
