using System.Text;
using YumGo.Domain.Common;

namespace YumGo.Application.Restaurants;

public sealed class RestaurantDiscoveryHandler(IRestaurantDiscoveryStore store)
{
    public async Task<RestaurantDiscoveryResult> SearchAsync(
        string? search, string? city, string? district, bool? openNow,
        string? categoryId, string? cursor, int? limit, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedLimit = limit ?? 20;
        if (normalizedLimit is < 1 or > 50)
            errors["limit"] = ["Limit must be between 1 and 50."];
        if (search?.Length > 120) errors["q"] = ["Search must be at most 120 characters."];
        if (city?.Length > 100) errors["city"] = ["City must be at most 100 characters."];
        if (district?.Length > 100) errors["district"] = ["District must be at most 100 characters."];
        if (categoryId is not null && !TryParseId(categoryId, out _)) errors["categoryId"] = ["Category ID must be a valid NanoID."];
        DateTimeOffset? cursorCreatedAt = null;
        string? cursorId = null;
        if (cursor is not null)
        {
            if (!TryDecodeCursor(cursor, out var decoded)) errors["cursor"] = ["Cursor is invalid."];
            else
            {
                var parts = decoded.Split('|');
                cursorCreatedAt = DateTimeOffset.Parse(parts[0]);
                cursorId = parts[1];
            }
        }
        if (errors.Count > 0)
            return new(RestaurantDiscoveryStatus.ValidationFailed, Errors: errors);

        var query = new RestaurantDiscoveryQuery(search?.Trim(), city?.Trim(), district?.Trim(), openNow,
            categoryId, cursorCreatedAt, cursorId, normalizedLimit);
        return new(RestaurantDiscoveryStatus.Success, Page: await store.SearchAsync(query, cancellationToken));
    }

    public async Task<RestaurantDiscoveryResult> GetPublicMenuAsync(string branchId, CancellationToken cancellationToken)
    {
        if (!TryParseId(branchId, out var id))
            return new(RestaurantDiscoveryStatus.ValidationFailed, Errors: new Dictionary<string, string[]> { ["branchId"] = ["Branch ID must be a valid NanoID."] });
        var menu = await store.GetPublicMenuAsync(id, cancellationToken);
        return menu is null
            ? new(RestaurantDiscoveryStatus.NotFound)
            : new(RestaurantDiscoveryStatus.Success, Menu: menu);
    }

    public async Task<RestaurantDiscoveryResult> GetRestaurantAsync(string restaurantId, CancellationToken cancellationToken)
    {
        if (!TryParseId(restaurantId, out var id)) return InvalidId("restaurantId");
        var restaurant = await store.GetRestaurantAsync(id, cancellationToken);
        return restaurant is null ? new(RestaurantDiscoveryStatus.NotFound) : new(RestaurantDiscoveryStatus.Success, Restaurant: restaurant);
    }

    public async Task<RestaurantDiscoveryResult> GetBranchesAsync(string restaurantId, CancellationToken cancellationToken)
    {
        if (!TryParseId(restaurantId, out var id)) return InvalidId("restaurantId");
        if (await store.GetRestaurantAsync(id, cancellationToken) is null)
            return new(RestaurantDiscoveryStatus.NotFound);
        var branches = await store.GetBranchesAsync(id, cancellationToken);
        return new(RestaurantDiscoveryStatus.Success, Branches: branches);
    }

    public async Task<RestaurantDiscoveryResult> GetBranchAsync(string branchId, CancellationToken cancellationToken)
    {
        if (!TryParseId(branchId, out var id)) return InvalidId("branchId");
        var branch = await store.GetBranchAsync(id, cancellationToken);
        return branch is null ? new(RestaurantDiscoveryStatus.NotFound) : new(RestaurantDiscoveryStatus.Success, Branch: branch);
    }

    private static RestaurantDiscoveryResult InvalidId(string field) => new(
        RestaurantDiscoveryStatus.ValidationFailed,
        Errors: new Dictionary<string, string[]> { [field] = ["ID must be a valid NanoID."] });

    private static bool TryParseId(string value, out NanoId id)
    {
        try { id = NanoId.Create(value); return true; }
        catch (ArgumentException) { id = null!; return false; }
    }

    private static bool TryDecodeCursor(string cursor, out string decoded)
    {
        decoded = "";
        if (cursor.Length is < 1 or > 256) return false;
        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            var parts = decoded.Split('|');
            return parts.Length == 2 && DateTimeOffset.TryParse(parts[0], out _) && TryParseId(parts[1], out _);
        }
        catch (FormatException) { return false; }
    }
}
