using System.Text;
using Microsoft.EntityFrameworkCore;
using YumGo.Application.Restaurants;
using YumGo.Domain.Common;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Restaurants;

public sealed class EfRestaurantDiscoveryStore(ApplicationDbContext dbContext) : IRestaurantDiscoveryStore
{
    public async Task<RestaurantDiscoveryPage> SearchAsync(RestaurantDiscoveryQuery query, CancellationToken cancellationToken)
    {
        var cursorCreatedAt = query.CursorCreatedAt ?? DateTimeOffset.MaxValue;
        var cursorId = query.CursorId ?? "~~~~~~~~~~~~~~~~~~~~~";
        var search = query.Search ?? "";
        var city = query.City ?? "";
        var district = query.District ?? "";
        var openNow = query.OpenNow ?? false;
        var categoryId = query.CategoryId ?? "";
        var rows = await dbContext.Database.SqlQuery<DiscoveryRow>($"""
            SELECT r.id AS "RestaurantId", r.name AS "RestaurantName", r.description AS "Description",
                   b.id AS "BranchId", b.name AS "BranchName", b.district AS "District", b.city AS "City",
                   b.latitude AS "Latitude", b.longitude AS "Longitude", b.status AS "Status",
                   b.opens_at AS "OpensAt", b.closes_at AS "ClosesAt", b.created_at AS "CreatedAt"
            FROM restaurants r
            INNER JOIN restaurant_branches b ON b.restaurant_id = r.id
            WHERE r.status = 'active' AND r.deleted_at IS NULL
              AND b.status = 'open' AND b.deleted_at IS NULL
              AND ({search} = '' OR r.name ILIKE '%' || {search} || '%' OR b.name ILIKE '%' || {search} || '%')
              AND ({city} = '' OR lower(b.city) = lower({city}))
              AND ({district} = '' OR lower(b.district) = lower({district}))
              AND (NOT {openNow} OR b.opens_at IS NULL
                   OR (b.opens_at <= (now() AT TIME ZONE 'UTC')::time AND b.closes_at > (now() AT TIME ZONE 'UTC')::time))
              AND ({categoryId} = '' OR EXISTS (
                   SELECT 1 FROM menu_categories c JOIN menu_items i ON i.category_id = c.id AND i.branch_id = c.branch_id
                   WHERE c.branch_id = b.id AND c.id = {categoryId} AND c.status = 'active' AND c.deleted_at IS NULL
                     AND i.status = 'active' AND i.stock_status = 'available' AND i.deleted_at IS NULL))
              AND (b.created_at < {cursorCreatedAt} OR (b.created_at = {cursorCreatedAt} AND b.id > {cursorId}))
            ORDER BY b.created_at DESC, b.id ASC
            LIMIT {query.Limit + 1}
            """).ToListAsync(cancellationToken);

        var hasMore = rows.Count > query.Limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        var items = rows.Select(row => new RestaurantDiscoverySummary(
            row.RestaurantId, row.RestaurantName, row.Description, row.BranchId, row.BranchName,
            row.District, row.City, row.Latitude, row.Longitude, row.Status, row.OpensAt, row.ClosesAt)).ToArray();
        var last = rows.LastOrDefault();
        return new RestaurantDiscoveryPage(items,
            hasMore && last is not null ? EncodeCursor(last.CreatedAt, last.BranchId) : null, hasMore);
    }

    public async Task<PublicMenu?> GetPublicMenuAsync(NanoId branchId, CancellationToken cancellationToken)
    {
        var branchExists = await dbContext.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value"
            FROM restaurant_branches b JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.id = {branchId.Value} AND b.status = 'open' AND b.deleted_at IS NULL
              AND r.status = 'active' AND r.deleted_at IS NULL
            """).SingleAsync(cancellationToken) > 0;
        if (!branchExists) return null;

        var rows = await dbContext.Database.SqlQuery<MenuRow>($"""
            SELECT c.id AS "CategoryId", c.name AS "CategoryName", c.display_order AS "DisplayOrder",
                   i.id AS "ItemId", i.name AS "ItemName", i.description AS "ItemDescription",
                   i.price AS "Price", i.stock_status AS "StockStatus"
            FROM menu_categories c
            LEFT JOIN menu_items i ON i.category_id = c.id AND i.branch_id = c.branch_id
                 AND i.status = 'active' AND i.stock_status = 'available' AND i.deleted_at IS NULL
            WHERE c.branch_id = {branchId.Value} AND c.status = 'active' AND c.deleted_at IS NULL
            ORDER BY c.display_order, c.id, i.created_at, i.id
            """).ToListAsync(cancellationToken);

        var categories = rows.GroupBy(row => new { row.CategoryId, row.CategoryName, row.DisplayOrder })
            .Select(group => new PublicMenuCategory(group.Key.CategoryId, group.Key.CategoryName, group.Key.DisplayOrder,
                group.Where(row => row.ItemId is not null).Select(row => new PublicMenuItem(
                    row.ItemId!, row.ItemName!, row.ItemDescription, row.Price!.Value, row.StockStatus!)).ToArray()))
            .ToArray();
        return new PublicMenu(categories);
    }

    public async Task<PublicRestaurant?> GetRestaurantAsync(NanoId restaurantId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Database.SqlQuery<RestaurantRow>($"""
            SELECT id AS "Id", name AS "Name", description AS "Description"
            FROM restaurants WHERE id = {restaurantId.Value} AND status = 'active' AND deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new PublicRestaurant(row.Id, row.Name, row.Description);
    }

    public async Task<IReadOnlyList<PublicBranch>> GetBranchesAsync(NanoId restaurantId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database.SqlQuery<PublicBranchRow>($"""
            SELECT b.id AS "Id", r.id AS "RestaurantId", r.name AS "RestaurantName", b.name AS "Name",
                   b.phone AS "Phone", b.address_line AS "AddressLine", b.ward AS "Ward", b.district AS "District",
                   b.city AS "City", b.latitude AS "Latitude", b.longitude AS "Longitude", b.status AS "Status",
                   b.opens_at AS "OpensAt", b.closes_at AS "ClosesAt"
            FROM restaurant_branches b JOIN restaurants r ON r.id = b.restaurant_id
            WHERE r.id = {restaurantId.Value} AND r.status = 'active' AND r.deleted_at IS NULL
              AND b.status = 'open' AND b.deleted_at IS NULL
            ORDER BY b.name, b.id
            """).ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<PublicBranch?> GetBranchAsync(NanoId branchId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Database.SqlQuery<PublicBranchRow>($"""
            SELECT b.id AS "Id", r.id AS "RestaurantId", r.name AS "RestaurantName", b.name AS "Name",
                   b.phone AS "Phone", b.address_line AS "AddressLine", b.ward AS "Ward", b.district AS "District",
                   b.city AS "City", b.latitude AS "Latitude", b.longitude AS "Longitude", b.status AS "Status",
                   b.opens_at AS "OpensAt", b.closes_at AS "ClosesAt"
            FROM restaurant_branches b JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.id = {branchId.Value} AND r.status = 'active' AND r.deleted_at IS NULL
              AND b.status = 'open' AND b.deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    private static PublicBranch Map(PublicBranchRow row) => new(
        row.Id, row.RestaurantId, row.RestaurantName, row.Name, row.Phone, row.AddressLine,
        row.Ward, row.District, row.City, row.Latitude, row.Longitude, row.Status, row.OpensAt, row.ClosesAt);

    private static string EncodeCursor(DateTimeOffset createdAt, string id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{createdAt:O}|{id}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class DiscoveryRow
    {
        public string RestaurantId { get; init; } = "";
        public string RestaurantName { get; init; } = "";
        public string? Description { get; init; }
        public string BranchId { get; init; } = "";
        public string BranchName { get; init; } = "";
        public string District { get; init; } = "";
        public string City { get; init; } = "";
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public string Status { get; init; } = "";
        public TimeOnly? OpensAt { get; init; }
        public TimeOnly? ClosesAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class MenuRow
    {
        public string CategoryId { get; init; } = "";
        public string CategoryName { get; init; } = "";
        public int DisplayOrder { get; init; }
        public string? ItemId { get; init; }
        public string? ItemName { get; init; }
        public string? ItemDescription { get; init; }
        public decimal? Price { get; init; }
        public string? StockStatus { get; init; }
    }

    private sealed class RestaurantRow
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Description { get; init; }
    }

    private sealed class PublicBranchRow
    {
        public string Id { get; init; } = "";
        public string RestaurantId { get; init; } = "";
        public string RestaurantName { get; init; } = "";
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
    }
}
