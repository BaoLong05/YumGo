using Microsoft.EntityFrameworkCore;
using Npgsql;
using YumGo.Application.Restaurants;
using YumGo.Domain.Common;
using YumGo.Domain.Restaurants;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Restaurants;

public sealed class EfRestaurantBranchStore(ApplicationDbContext dbContext) : IRestaurantBranchStore
{
    public async Task<IReadOnlyList<Restaurant>> ListRestaurantsAsync(NanoId ownerId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database.SqlQuery<RestaurantRow>($"""
            SELECT id AS "Id", owner_user_id AS "OwnerUserId", name AS "Name", description AS "Description",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM restaurants
            WHERE owner_user_id = {ownerId.Value} AND deleted_at IS NULL
            ORDER BY created_at DESC, id
            """).ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<Restaurant?> FindRestaurantAsync(NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Database.SqlQuery<RestaurantRow>($"""
            SELECT id AS "Id", owner_user_id AS "OwnerUserId", name AS "Name", description AS "Description",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM restaurants
            WHERE id = {restaurantId.Value} AND owner_user_id = {ownerId.Value} AND deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task CreateRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO restaurants (id, owner_user_id, name, description, status, created_at, updated_at)
            VALUES ({restaurant.Id.Value}, {restaurant.OwnerUserId.Value}, {restaurant.Name}, {restaurant.Description},
                    {restaurant.Status}, {restaurant.CreatedAt}, {restaurant.UpdatedAt})
            """, cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, after_json, created_at)
            VALUES ({NanoId.New().Value}, {restaurant.OwnerUserId.Value}, 'restaurant.created', 'restaurant',
                    {restaurant.Id.Value}, jsonb_build_object('name', {restaurant.Name}, 'status', {restaurant.Status}),
                    {restaurant.CreatedAt})
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RestaurantBranch>> ListBranchesAsync(
        NanoId restaurantId, NanoId ownerId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database.SqlQuery<BranchRow>($"""
            SELECT b.id AS "Id", b.restaurant_id AS "RestaurantId", b.name AS "Name", b.phone AS "Phone",
                   b.address_line AS "AddressLine", b.ward AS "Ward", b.district AS "District", b.city AS "City",
                   b.latitude AS "Latitude", b.longitude AS "Longitude", b.status AS "Status",
                   b.opens_at AS "OpensAt", b.closes_at AS "ClosesAt", b.created_at AS "CreatedAt", b.updated_at AS "UpdatedAt"
            FROM restaurant_branches b
            INNER JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.restaurant_id = {restaurantId.Value} AND r.owner_user_id = {ownerId.Value}
              AND b.deleted_at IS NULL AND r.deleted_at IS NULL
            ORDER BY b.created_at, b.id
            """).ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task CreateBranchAsync(RestaurantBranch branch, NanoId ownerId, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var lockedRestaurantId = await dbContext.Database.SqlQuery<string>($"""
                SELECT id AS "Value"
                FROM restaurants
                WHERE id = {branch.RestaurantId.Value}
                  AND owner_user_id = {ownerId.Value}
                  AND deleted_at IS NULL
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);
            if (lockedRestaurantId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new RestaurantNotFoundException();
            }

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO restaurant_branches (
                    id, restaurant_id, name, phone, address_line, ward, district, city, latitude, longitude,
                    status, opens_at, closes_at, created_at, updated_at)
                VALUES (
                    {branch.Id.Value}, {branch.RestaurantId.Value}, {branch.Name}, {branch.Phone.Value},
                    {branch.AddressLine}, {branch.Ward}, {branch.District}, {branch.City}, {branch.Latitude},
                    {branch.Longitude}, {branch.Status}, {branch.OpensAt}, {branch.ClosesAt}, {branch.CreatedAt}, {branch.UpdatedAt})
                """, cancellationToken);
            var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, after_json, created_at)
                SELECT {NanoId.New().Value}, r.owner_user_id, 'restaurant.branch.created', 'restaurant_branch',
                       {branch.Id.Value}, jsonb_build_object('restaurantId', r.id, 'name', {branch.Name}, 'status', {branch.Status}),
                       {branch.CreatedAt}
                FROM restaurants r
                WHERE r.id = {branch.RestaurantId.Value}
                  AND r.owner_user_id = {ownerId.Value}
                  AND r.deleted_at IS NULL
                """, cancellationToken);
            if (auditRows != 1)
                throw new RestaurantNotFoundException();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_branch_name_per_restaurant" })
        {
            throw new RestaurantBranchNameConflictException();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation && exception.ConstraintName == "ux_branch_name_per_restaurant")
        {
            throw new RestaurantBranchNameConflictException();
        }
    }

    private static Restaurant Map(RestaurantRow row) => Restaurant.Restore(
        NanoId.Create(row.Id), NanoId.Create(row.OwnerUserId), row.Name, row.Description, row.Status, row.CreatedAt, row.UpdatedAt);

    private static RestaurantBranch Map(BranchRow row) => RestaurantBranch.Restore(
        NanoId.Create(row.Id), NanoId.Create(row.RestaurantId), row.Name, row.Phone, row.AddressLine,
        row.Ward, row.District, row.City, row.Latitude, row.Longitude, row.Status, row.OpensAt, row.ClosesAt,
        row.CreatedAt, row.UpdatedAt);

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
