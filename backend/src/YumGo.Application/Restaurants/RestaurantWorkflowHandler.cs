using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Application.Restaurants;

public sealed class RestaurantWorkflowHandler(
    IRestaurantWorkflowStore store,
    TimeProvider timeProvider)
{
    private static readonly HashSet<string> AllowedStaffRoles = new(StringComparer.Ordinal)
    {
        "manager",
        "staff"
    };

    public async Task<RestaurantWorkflowResult> ReviewRestaurantAsync(
        string? adminUserId, string? restaurantId, string? decision, string? reason,
        CancellationToken cancellationToken)
    {
        if (!TryNanoId(adminUserId, out var adminId)) return Invalid("adminUserId", "A valid user ID is required.");
        if (!TryNanoId(restaurantId, out var parsedRestaurantId)) return Invalid("restaurantId", "A valid restaurant ID is required.");

        var normalizedDecision = decision?.Trim().ToLowerInvariant();
        if (normalizedDecision is not ("approve" or "reject"))
            return Invalid("decision", "Decision must be 'approve' or 'reject'.");

        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedDecision == "reject" && normalizedReason is null)
            return Invalid("reason", "A rejection reason is required.");
        if (normalizedReason?.Length > 500)
            return Invalid("reason", "The reason cannot exceed 500 characters.");

        var result = await store.ReviewRestaurantAsync(parsedRestaurantId!, adminId!,
            normalizedDecision == "approve", normalizedReason, timeProvider.GetUtcNow(), cancellationToken);
        return Map(result, restaurant => new RestaurantWorkflowResult(
            RestaurantWorkflowStatus.Success, Restaurant: ToDto(restaurant)));
    }

    public async Task<RestaurantWorkflowResult> SetBranchVisibilityAsync(
        string? ownerUserId, string? restaurantId, string? branchId, string? status,
        CancellationToken cancellationToken)
    {
        if (!TryNanoId(ownerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        if (!TryNanoId(restaurantId, out var parsedRestaurantId)) return Invalid("restaurantId", "A valid restaurant ID is required.");
        if (!TryNanoId(branchId, out var parsedBranchId)) return Invalid("branchId", "A valid branch ID is required.");

        var normalizedStatus = status?.Trim().ToLowerInvariant();
        if (normalizedStatus is not ("open" or "closed"))
            return Invalid("status", "Status must be 'open' or 'closed'.");

        var result = await store.SetBranchVisibilityAsync(parsedRestaurantId!, parsedBranchId!, ownerId!,
            normalizedStatus == "open", timeProvider.GetUtcNow(), cancellationToken);
        return Map(result, branch => new RestaurantWorkflowResult(
            RestaurantWorkflowStatus.Success, Branch: ToDto(branch)));
    }

    public async Task<RestaurantWorkflowResult> ReplaceStaffRolesAsync(
        string? ownerUserId, string? restaurantId, string? employeeUserId, IReadOnlyList<string?>? roles,
        CancellationToken cancellationToken)
    {
        if (!TryNanoId(ownerUserId, out var ownerId)) return Invalid("ownerUserId", "A valid user ID is required.");
        if (!TryNanoId(restaurantId, out var parsedRestaurantId)) return Invalid("restaurantId", "A valid restaurant ID is required.");
        if (!TryNanoId(employeeUserId, out var parsedEmployeeId)) return Invalid("employeeUserId", "A valid user ID is required.");
        if (roles is null) return Invalid("roles", "A roles array is required; use an empty array to remove all roles.");
        if (roles.Count > AllowedStaffRoles.Count) return Invalid("roles", "At most two restaurant roles can be assigned.");

        var normalizedRoles = new List<string>(roles.Count);
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role)) return Invalid("roles", "Role values cannot be empty.");
            var normalizedRole = role.Trim().ToLowerInvariant();
            if (!AllowedStaffRoles.Contains(normalizedRole))
                return Invalid("roles", "Allowed restaurant roles are 'manager' and 'staff'.");
            if (normalizedRoles.Contains(normalizedRole, StringComparer.Ordinal))
                return Invalid("roles", "A role can only be selected once.");
            normalizedRoles.Add(normalizedRole);
        }

        var result = await store.ReplaceStaffRolesAsync(parsedRestaurantId!, ownerId!, parsedEmployeeId!,
            normalizedRoles, timeProvider.GetUtcNow(), cancellationToken);
        return Map(result, staff => new RestaurantWorkflowResult(
            RestaurantWorkflowStatus.Success, Staff: staff));
    }

    private static RestaurantWorkflowResult Map<T>(
        RestaurantWorkflowStoreResult<T> result,
        Func<T, RestaurantWorkflowResult> success)
    {
        return result.Status switch
        {
            RestaurantWorkflowStoreStatus.Updated when result.Value is not null => success(result.Value),
            RestaurantWorkflowStoreStatus.Updated => throw new InvalidOperationException("A successful workflow mutation must return its updated resource."),
            RestaurantWorkflowStoreStatus.NotFound => new(RestaurantWorkflowStatus.NotFound),
            RestaurantWorkflowStoreStatus.Conflict => new(RestaurantWorkflowStatus.Conflict),
            RestaurantWorkflowStoreStatus.Forbidden => new(RestaurantWorkflowStatus.Forbidden),
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
    }

    private static bool TryNanoId(string? value, out NanoId? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try { id = NanoId.Create(value); return true; }
        catch (ArgumentException) { return false; }
    }

    private static RestaurantWorkflowResult Invalid(string field, string message) =>
        new(RestaurantWorkflowStatus.ValidationFailed,
            Errors: new Dictionary<string, string[]> { [field] = [message] });

    private static RestaurantDto ToDto(Restaurant restaurant) => new(
        restaurant.Id.Value, restaurant.Name, restaurant.Description, restaurant.Status,
        restaurant.CreatedAt, restaurant.UpdatedAt);

    private static RestaurantBranchDto ToDto(RestaurantBranch branch) => new(
        branch.Id.Value, branch.RestaurantId.Value, branch.Name, branch.Phone.Value,
        branch.AddressLine, branch.Ward, branch.District, branch.City, branch.Latitude, branch.Longitude,
        branch.Status, branch.OpensAt, branch.ClosesAt, branch.CreatedAt, branch.UpdatedAt);
}
