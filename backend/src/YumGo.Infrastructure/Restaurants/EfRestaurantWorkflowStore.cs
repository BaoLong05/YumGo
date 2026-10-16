using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YumGo.Application.Restaurants;
using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Restaurants;

public sealed class EfRestaurantWorkflowStore(ApplicationDbContext dbContext) : IRestaurantWorkflowStore
{
    public async Task<RestaurantWorkflowStoreResult<Restaurant>> ReviewRestaurantAsync(
        NanoId restaurantId, NanoId adminUserId, bool approve, string? reason, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var row = await dbContext.Database.SqlQuery<RestaurantRow>($"""
            SELECT id AS "Id", owner_user_id AS "OwnerUserId", name AS "Name", description AS "Description",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM restaurants
            WHERE id = {restaurantId.Value} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.NotFound);
        }

        if (!await HasAdministratorRoleAsync(adminUserId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Forbidden);
        }

        if (row.Status != "pending")
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Conflict);
        }

        var reviewed = Map(row).Review(approve, now);
        var updatedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE restaurants
            SET status = {reviewed.Status}, updated_at = {now}
            WHERE id = {restaurantId.Value} AND status = 'pending' AND deleted_at IS NULL
            """, cancellationToken);
        if (updatedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Conflict);
        }

        var action = approve ? "restaurant.review.approved" : "restaurant.review.rejected";
        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
            VALUES ({NanoId.New().Value}, {adminUserId.Value}, {action}, 'restaurant', {restaurantId.Value},
                    jsonb_build_object('status', 'pending'),
                    jsonb_build_object('status', {reviewed.Status}, 'reason', CAST({reason} AS text)), {now})
            """, cancellationToken);
        if (auditRows != 1)
            throw new InvalidOperationException("The restaurant review audit event was not written.");

        await transaction.CommitAsync(cancellationToken);
        return new(RestaurantWorkflowStoreStatus.Updated, reviewed);
    }

    public async Task<RestaurantWorkflowStoreResult<RestaurantBranch>> SetBranchVisibilityAsync(
        NanoId restaurantId, NanoId branchId, NanoId ownerUserId, bool isOpen, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var restaurant = await dbContext.Database.SqlQuery<RestaurantRow>($"""
            SELECT id AS "Id", owner_user_id AS "OwnerUserId", name AS "Name", description AS "Description",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM restaurants
            WHERE id = {restaurantId.Value} AND owner_user_id = {ownerUserId.Value} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (restaurant is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.NotFound);
        }

        if (!await IsVerifiedActiveUserAsync(ownerUserId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Forbidden);
        }

        var branchRow = await dbContext.Database.SqlQuery<BranchRow>($"""
            SELECT id AS "Id", restaurant_id AS "RestaurantId", name AS "Name", phone AS "Phone",
                   address_line AS "AddressLine", ward AS "Ward", district AS "District", city AS "City",
                   latitude AS "Latitude", longitude AS "Longitude", status AS "Status",
                   opens_at AS "OpensAt", closes_at AS "ClosesAt", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM restaurant_branches
            WHERE id = {branchId.Value} AND restaurant_id = {restaurantId.Value} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (branchRow is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.NotFound);
        }

        if ((isOpen && restaurant.Status != "active") || branchRow.Status == "suspended")
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Conflict);
        }

        var branch = Map(branchRow).SetVisibility(isOpen, now);
        if (branch.Status == branchRow.Status)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Updated, branch);
        }

        var updatedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE restaurant_branches
            SET status = {branch.Status}, updated_at = {now}
            WHERE id = {branchId.Value} AND restaurant_id = {restaurantId.Value}
              AND status = {branchRow.Status} AND deleted_at IS NULL
            """, cancellationToken);
        if (updatedRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Conflict);
        }

        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
            VALUES ({NanoId.New().Value}, {ownerUserId.Value}, 'restaurant.branch.status.changed', 'restaurant_branch',
                    {branchId.Value}, jsonb_build_object('status', {branchRow.Status}),
                    jsonb_build_object('status', {branch.Status}), {now})
            """, cancellationToken);
        if (auditRows != 1)
            throw new InvalidOperationException("The branch status audit event was not written.");

        await transaction.CommitAsync(cancellationToken);
        return new(RestaurantWorkflowStoreStatus.Updated, branch);
    }

    public async Task<RestaurantWorkflowStoreResult<RestaurantStaffRolesDto>> ReplaceStaffRolesAsync(
        NanoId restaurantId, NanoId ownerUserId, NanoId employeeUserId, IReadOnlyList<string> roles,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var restaurant = await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value"
            FROM restaurants
            WHERE id = {restaurantId.Value} AND owner_user_id = {ownerUserId.Value} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (restaurant is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.NotFound);
        }

        if (!await IsVerifiedActiveUserAsync(ownerUserId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Forbidden);
        }

        var eligibleEmployee = await IsVerifiedActiveUserAsync(employeeUserId, cancellationToken);
        if (!eligibleEmployee)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.NotFound);
        }

        var previousRoles = await dbContext.Database.SqlQuery<string>($"""
            SELECT role AS "Value"
            FROM restaurant_staff
            WHERE restaurant_id = {restaurantId.Value} AND user_id = {employeeUserId.Value} AND status = 'active'
            ORDER BY role
            FOR UPDATE
            """).ToArrayAsync(cancellationToken);
        var normalizedRoles = roles.Order(StringComparer.Ordinal).ToArray();
        if (previousRoles.SequenceEqual(normalizedRoles, StringComparer.Ordinal))
        {
            await transaction.CommitAsync(cancellationToken);
            return new(RestaurantWorkflowStoreStatus.Updated, new(employeeUserId.Value, previousRoles));
        }

        foreach (var role in previousRoles.Except(normalizedRoles, StringComparer.Ordinal))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE restaurant_staff
                SET status = 'inactive'
                WHERE restaurant_id = {restaurantId.Value} AND user_id = {employeeUserId.Value}
                  AND role = {role} AND status = 'active'
                """, cancellationToken);
        }

        foreach (var role in normalizedRoles)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO restaurant_staff (id, restaurant_id, user_id, role, status, created_at)
                VALUES ({NanoId.New().Value}, {restaurantId.Value}, {employeeUserId.Value}, {role}, 'active', {now})
                ON CONFLICT (restaurant_id, user_id, role)
                DO UPDATE SET status = 'active'
                """, cancellationToken);
        }

        var beforeJson = JsonSerializer.Serialize(previousRoles);
        var afterJson = JsonSerializer.Serialize(normalizedRoles);
        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
            VALUES ({NanoId.New().Value}, {ownerUserId.Value}, 'restaurant.staff.roles.updated', 'restaurant_staff',
                    {employeeUserId.Value}, CAST({beforeJson} AS jsonb), CAST({afterJson} AS jsonb), {now})
            """, cancellationToken);
        if (auditRows != 1)
            throw new InvalidOperationException("The staff role audit event was not written.");

        await transaction.CommitAsync(cancellationToken);
        return new(RestaurantWorkflowStoreStatus.Updated, new(employeeUserId.Value, normalizedRoles));
    }

    private async Task<bool> IsVerifiedActiveUserAsync(NanoId userId, CancellationToken cancellationToken) =>
        await dbContext.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1 FROM users
                WHERE id = {userId.Value} AND status = 'active' AND deleted_at IS NULL
                  AND email_verified_at IS NOT NULL
            ) AS "Value"
            """).SingleAsync(cancellationToken);

    private async Task<bool> HasAdministratorRoleAsync(NanoId userId, CancellationToken cancellationToken) =>
        await dbContext.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM users u
                JOIN user_roles ur ON ur.user_id = u.id
                JOIN roles r ON r.id = ur.role_id
                WHERE u.id = {userId.Value} AND u.status = 'active'
                  AND u.deleted_at IS NULL AND u.email_verified_at IS NOT NULL
                  AND r.code = 'admin'
            ) AS "Value"
            """).SingleAsync(cancellationToken);

    private static Restaurant Map(RestaurantRow row) => Restaurant.Restore(
        NanoId.Create(row.Id), NanoId.Create(row.OwnerUserId), row.Name, row.Description, row.Status,
        row.CreatedAt, row.UpdatedAt);

    private static RestaurantBranch Map(BranchRow row) => RestaurantBranch.Restore(
        NanoId.Create(row.Id), NanoId.Create(row.RestaurantId), row.Name, row.Phone, row.AddressLine,
        row.Ward, row.District, row.City, row.Latitude, row.Longitude, row.Status, row.OpensAt,
        row.ClosesAt, row.CreatedAt, row.UpdatedAt);

    private sealed class RestaurantRow
    {
        public string Id { get; init; } = "";
        public string OwnerUserId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public string Status { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }

    private sealed class BranchRow
    {
        public string Id { get; init; } = "";
        public string RestaurantId { get; init; } = "";
        public string Name { get; init; } = "";
        public string Phone { get; init; } = "";
        public string AddressLine { get; init; } = "";
        public string Ward { get; init; } = "";
        public string District { get; init; } = "";
        public string City { get; init; } = "";
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public string Status { get; init; } = "";
        public TimeOnly? OpensAt { get; init; }
        public TimeOnly? ClosesAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
