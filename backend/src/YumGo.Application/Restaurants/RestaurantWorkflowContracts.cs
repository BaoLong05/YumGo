using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;

namespace YumGo.Application.Restaurants;

public interface IRestaurantWorkflowStore
{
    Task<RestaurantWorkflowStoreResult<Restaurant>> ReviewRestaurantAsync(
        NanoId restaurantId, NanoId adminUserId, bool approve, string? reason, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<RestaurantWorkflowStoreResult<RestaurantBranch>> SetBranchVisibilityAsync(
        NanoId restaurantId, NanoId branchId, NanoId ownerUserId, bool isOpen, DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<RestaurantWorkflowStoreResult<RestaurantStaffRolesDto>> ReplaceStaffRolesAsync(
        NanoId restaurantId, NanoId ownerUserId, NanoId employeeUserId, IReadOnlyList<string> roles,
        DateTimeOffset now, CancellationToken cancellationToken);
}

public enum RestaurantWorkflowStoreStatus { Updated, NotFound, Conflict, Forbidden }

public sealed record RestaurantWorkflowStoreResult<T>(RestaurantWorkflowStoreStatus Status, T? Value = default);

public enum RestaurantWorkflowStatus { Success, ValidationFailed, NotFound, Conflict, Forbidden }

public sealed record RestaurantWorkflowResult(
    RestaurantWorkflowStatus Status,
    RestaurantDto? Restaurant = null,
    RestaurantBranchDto? Branch = null,
    RestaurantStaffRolesDto? Staff = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record RestaurantStaffRolesDto(string UserId, IReadOnlyList<string> Roles);
