using Microsoft.EntityFrameworkCore;
using Npgsql;
using YumGo.Application.Menu;
using YumGo.Domain.Common;
using YumGo.Domain.Menu;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Menu;

public sealed class EfRestaurantMenuStore(ApplicationDbContext dbContext) : IRestaurantMenuStore
{
    public async Task<MenuStoreResult<RestaurantMenuDto>> GetBranchMenuAsync(
        NanoId branchId, NanoId actorUserId, CancellationToken cancellationToken)
    {
        var branch = await dbContext.Database.SqlQuery<AuthorizedBranchRow>($"""
            SELECT b.id AS "BranchId", r.id AS "RestaurantId", r.owner_user_id AS "OwnerUserId"
            FROM restaurant_branches b
            INNER JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.id = {branchId.Value} AND b.deleted_at IS NULL AND r.deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branch is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken)
            || !await HasRestaurantAccessAsync(branch.RestaurantId, branch.OwnerUserId, actorUserId, cancellationToken))
            return new(MenuStoreStatus.NotFound);

        var categories = await dbContext.Database.SqlQuery<MenuCategoryRow>($"""
            SELECT id AS "Id", branch_id AS "BranchId", name AS "Name", display_order AS "DisplayOrder",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM menu_categories
            WHERE branch_id = {branchId.Value} AND deleted_at IS NULL
            ORDER BY display_order, id
            """).ToListAsync(cancellationToken);
        var items = await dbContext.Database.SqlQuery<MenuItemRow>($"""
            SELECT id AS "Id", branch_id AS "BranchId", category_id AS "CategoryId", name AS "Name",
                   description AS "Description", price AS "Price", stock_status AS "StockStatus",
                   status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM menu_items
            WHERE branch_id = {branchId.Value} AND deleted_at IS NULL
            ORDER BY category_id, name, id
            """).ToListAsync(cancellationToken);

        var itemsByCategory = items.GroupBy(item => item.CategoryId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<MenuItemDto>)group.Select(Map).ToArray());
        var categoryDtos = categories.Select(category => new MenuCategoryWithItemsDto(
            category.Id, category.BranchId, category.Name, category.DisplayOrder, category.Status,
            category.CreatedAt, category.UpdatedAt,
            itemsByCategory.GetValueOrDefault(category.Id, Array.Empty<MenuItemDto>()))).ToArray();
        return new(MenuStoreStatus.Success, new RestaurantMenuDto(branchId.Value, categoryDtos));
    }

    public async Task<MenuStoreResult<MenuCategory>> CreateCategoryAsync(
        MenuCategory category, NanoId actorUserId, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var branchRestaurantId = await dbContext.Database.SqlQuery<string>($"""
                SELECT restaurant_id AS "Value"
                FROM restaurant_branches
                WHERE id = {category.BranchId.Value} AND deleted_at IS NULL
                """).SingleOrDefaultAsync(cancellationToken);
            if (branchRestaurantId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            var restaurantOwnerId = await dbContext.Database.SqlQuery<string>($"""
                SELECT owner_user_id AS "Value"
                FROM restaurants
                WHERE id = {branchRestaurantId} AND deleted_at IS NULL
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);
            if (restaurantOwnerId is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }



            if (!string.Equals(restaurantOwnerId, actorUserId.Value, StringComparison.Ordinal)
                && !await IsActiveRestaurantStaffAsync(branchRestaurantId, actorUserId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            var lockedBranchId = await dbContext.Database.SqlQuery<string>($"""
                SELECT id AS "Value"
                FROM restaurant_branches
                WHERE id = {category.BranchId.Value} AND restaurant_id = {branchRestaurantId}
                  AND deleted_at IS NULL
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);
            if (lockedBranchId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO menu_categories (id, branch_id, name, display_order, status, created_at, updated_at)
                VALUES ({category.Id.Value}, {category.BranchId.Value}, {category.Name}, {category.DisplayOrder},
                        {category.Status}, {category.CreatedAt}, {category.UpdatedAt})
                """, cancellationToken);
            var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, after_json, created_at)
                VALUES ({NanoId.New().Value}, {actorUserId.Value}, 'restaurant.menu.category.created', 'menu_category',
                        {category.Id.Value},
                        jsonb_build_object('branchId', {category.BranchId.Value}, 'name', {category.Name},
                                           'displayOrder', {category.DisplayOrder}, 'status', {category.Status}),
                        {category.CreatedAt})
                """, cancellationToken);
            if (auditRows != 1) throw new InvalidOperationException("The menu category audit event was not written.");

            await transaction.CommitAsync(cancellationToken);
            return new(MenuStoreStatus.Created, category);
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation
            && exception.ConstraintName == "ux_category_name_per_branch")
        {
            return new(MenuStoreStatus.Conflict);
        }
    }

    public async Task<MenuStoreResult<MenuCategory>> UpdateCategoryAsync(
        NanoId categoryId, NanoId actorUserId, string name, int displayOrder, string status,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var branchId = await dbContext.Database.SqlQuery<string>($"""
                SELECT branch_id AS "Value" FROM menu_categories
                WHERE id = {categoryId.Value} AND deleted_at IS NULL
                """).SingleOrDefaultAsync(cancellationToken);
            if (branchId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            var branch = await dbContext.Database.SqlQuery<AuthorizedBranchRow>($"""
                SELECT b.id AS "BranchId", r.id AS "RestaurantId", r.owner_user_id AS "OwnerUserId"
                FROM restaurant_branches b
                INNER JOIN restaurants r ON r.id = b.restaurant_id
                WHERE b.id = {branchId} AND b.deleted_at IS NULL AND r.deleted_at IS NULL
                """).SingleOrDefaultAsync(cancellationToken);
            if (branch is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            var ownerId = await dbContext.Database.SqlQuery<string>($"""
                SELECT owner_user_id AS "Value" FROM restaurants
                WHERE id = {branch.RestaurantId} AND deleted_at IS NULL
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);
            if (ownerId is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken)
                || !await HasRestaurantAccessAsync(branch.RestaurantId, ownerId, actorUserId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }

            var categoryRow = await dbContext.Database.SqlQuery<MenuCategoryRow>($"""
                SELECT id AS "Id", branch_id AS "BranchId", name AS "Name", display_order AS "DisplayOrder",
                       status AS "Status", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
                FROM menu_categories
                WHERE id = {categoryId.Value} AND branch_id = {branchId} AND deleted_at IS NULL
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);
            if (categoryRow is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(MenuStoreStatus.NotFound);
            }



            var updatedCategory = MenuCategory.Restore(NanoId.Create(categoryRow.Id), NanoId.Create(categoryRow.BranchId),
                categoryRow.Name, categoryRow.DisplayOrder, categoryRow.Status, categoryRow.CreatedAt, categoryRow.UpdatedAt)
                .Update(name, displayOrder, status, now);
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE menu_categories
                SET name = {updatedCategory.Name}, display_order = {updatedCategory.DisplayOrder},
                    status = {updatedCategory.Status}, updated_at = {updatedCategory.UpdatedAt}
                WHERE id = {categoryId.Value} AND deleted_at IS NULL
                """, cancellationToken);

            var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
                VALUES ({NanoId.New().Value}, {actorUserId.Value}, 'restaurant.menu.category.updated', 'menu_category',
                        {categoryId.Value},
                        jsonb_build_object('name', {categoryRow.Name}, 'displayOrder', {categoryRow.DisplayOrder}, 'status', {categoryRow.Status}),
                        jsonb_build_object('name', {updatedCategory.Name}, 'displayOrder', {updatedCategory.DisplayOrder}, 'status', {updatedCategory.Status}),
                        {now})
                """, cancellationToken);
            if (auditRows != 1) throw new InvalidOperationException("The menu category audit event was not written.");

            await transaction.CommitAsync(cancellationToken);
            return new(MenuStoreStatus.Updated, updatedCategory);
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation
            && exception.ConstraintName == "ux_category_name_per_branch")
        {
            return new(MenuStoreStatus.Conflict);
        }
    }

    public async Task<MenuStoreResult<MenuItem>> CreateItemAsync(
        MenuItem item, NanoId actorUserId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var branchRestaurantId = await dbContext.Database.SqlQuery<string>($"""
            SELECT restaurant_id AS "Value" FROM restaurant_branches
            WHERE id = {item.BranchId.Value} AND deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branchRestaurantId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(MenuStoreStatus.NotFound);
        }

        var ownerId = await dbContext.Database.SqlQuery<string>($"""
            SELECT owner_user_id AS "Value" FROM restaurants
            WHERE id = {branchRestaurantId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (ownerId is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken)
            || !await HasRestaurantAccessAsync(branchRestaurantId, ownerId, actorUserId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(MenuStoreStatus.NotFound);
        }

        var lockedBranchId = await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value" FROM restaurant_branches
            WHERE id = {item.BranchId.Value} AND restaurant_id = {branchRestaurantId} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        var categoryStatus = await dbContext.Database.SqlQuery<string>($"""
            SELECT status AS "Value" FROM menu_categories
            WHERE id = {item.CategoryId.Value} AND branch_id = {item.BranchId.Value} AND deleted_at IS NULL
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (lockedBranchId is null || categoryStatus is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(MenuStoreStatus.NotFound);
        }
        if (categoryStatus != "active")
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(MenuStoreStatus.Conflict);
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO menu_items (id, branch_id, category_id, name, description, price, stock_status, status, created_at, updated_at)
            VALUES ({item.Id.Value}, {item.BranchId.Value}, {item.CategoryId.Value}, {item.Name}, {item.Description},
                    {item.Price}, {item.StockStatus}, {item.Status}, {item.CreatedAt}, {item.UpdatedAt})
            """, cancellationToken);
        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, after_json, created_at)
            VALUES ({NanoId.New().Value}, {actorUserId.Value}, 'restaurant.menu.item.created', 'menu_item',
                    {item.Id.Value}, jsonb_build_object('branchId', {item.BranchId.Value},
                        'categoryId', {item.CategoryId.Value}, 'name', {item.Name}, 'price', {item.Price},
                        'stockStatus', {item.StockStatus}, 'status', {item.Status}), {item.CreatedAt})
            """, cancellationToken);
        if (auditRows != 1) throw new InvalidOperationException("The menu item audit event was not written.");

        await transaction.CommitAsync(cancellationToken);
        return new(MenuStoreStatus.Created, item);
    }

    public async Task<MenuStoreResult<MenuItem>> UpdateItemAsync(
        NanoId itemId, NanoId actorUserId, NanoId categoryId, string name, string? description,
        decimal price, string status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var branchId = await dbContext.Database.SqlQuery<string>($"""
            SELECT branch_id AS "Value" FROM menu_items WHERE id = {itemId.Value} AND deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branchId is null) { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }

        var branch = await dbContext.Database.SqlQuery<AuthorizedBranchRow>($"""
            SELECT b.id AS "BranchId", r.id AS "RestaurantId", r.owner_user_id AS "OwnerUserId"
            FROM restaurant_branches b INNER JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.id = {branchId} AND b.deleted_at IS NULL AND r.deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branch is null) { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }

        var ownerId = await dbContext.Database.SqlQuery<string>($"""
            SELECT owner_user_id AS "Value" FROM restaurants
            WHERE id = {branch.RestaurantId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (ownerId is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken)
            || !await HasRestaurantAccessAsync(branch.RestaurantId, ownerId, actorUserId, cancellationToken))
        { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }

        var lockedBranchId = await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value" FROM restaurant_branches
            WHERE id = {branchId} AND restaurant_id = {branch.RestaurantId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        var row = await dbContext.Database.SqlQuery<MenuItemRow>($"""
            SELECT id AS "Id", branch_id AS "BranchId", category_id AS "CategoryId", name AS "Name",
                   description AS "Description", price AS "Price", stock_status AS "StockStatus", status AS "Status",
                   created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM menu_items WHERE id = {itemId.Value} AND branch_id = {branchId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        var categoryStatus = await dbContext.Database.SqlQuery<string>($"""
            SELECT status AS "Value" FROM menu_categories
            WHERE id = {categoryId.Value} AND branch_id = {branchId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (lockedBranchId is null || row is null || categoryStatus is null)
        { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }
        if (status == "active" && row.Status != "active" && categoryStatus != "active")
        { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.Conflict); }

        var current = MenuItem.Restore(NanoId.Create(row.Id), NanoId.Create(row.BranchId), NanoId.Create(row.CategoryId),
            row.Name, row.Description, row.Price, row.StockStatus, row.Status, row.CreatedAt, row.UpdatedAt);
        var updated = current.Update(categoryId, name, description, price, status, now);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE menu_items SET category_id = {updated.CategoryId.Value}, name = {updated.Name},
                description = {updated.Description}, price = {updated.Price}, status = {updated.Status}, updated_at = {updated.UpdatedAt}
            WHERE id = {itemId.Value} AND deleted_at IS NULL
            """, cancellationToken);
        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
            VALUES ({NanoId.New().Value}, {actorUserId.Value}, 'restaurant.menu.item.updated', 'menu_item', {itemId.Value},
                jsonb_build_object('categoryId', {row.CategoryId}, 'name', {row.Name}, 'description', {row.Description}, 'price', {row.Price}, 'status', {row.Status}, 'stockStatus', {row.StockStatus}),
                jsonb_build_object('categoryId', {updated.CategoryId.Value}, 'name', {updated.Name}, 'description', {updated.Description}, 'price', {updated.Price}, 'status', {updated.Status}, 'stockStatus', {updated.StockStatus}),
                {now})
            """, cancellationToken);
        if (auditRows != 1) throw new InvalidOperationException("The menu item audit event was not written.");
        await transaction.CommitAsync(cancellationToken);
        return new(MenuStoreStatus.Updated, updated);
    }

    public async Task<MenuStoreResult<MenuItem>> SetItemAvailabilityAsync(
        NanoId itemId, NanoId actorUserId, bool available, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var branchId = await dbContext.Database.SqlQuery<string>($"""
            SELECT branch_id AS "Value" FROM menu_items WHERE id = {itemId.Value} AND deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branchId is null) { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }
        var branch = await dbContext.Database.SqlQuery<AuthorizedBranchRow>($"""
            SELECT b.id AS "BranchId", r.id AS "RestaurantId", r.owner_user_id AS "OwnerUserId"
            FROM restaurant_branches b INNER JOIN restaurants r ON r.id = b.restaurant_id
            WHERE b.id = {branchId} AND b.deleted_at IS NULL AND r.deleted_at IS NULL
            """).SingleOrDefaultAsync(cancellationToken);
        if (branch is null) { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }
        var ownerId = await dbContext.Database.SqlQuery<string>($"""
            SELECT owner_user_id AS "Value" FROM restaurants
            WHERE id = {branch.RestaurantId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (ownerId is null || !await IsVerifiedActiveUserAsync(actorUserId, cancellationToken)
            || !await HasRestaurantAccessAsync(branch.RestaurantId, ownerId, actorUserId, cancellationToken))
        { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }
        var lockedBranchId = await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value" FROM restaurant_branches
            WHERE id = {branchId} AND restaurant_id = {branch.RestaurantId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        var row = await dbContext.Database.SqlQuery<MenuItemRow>($"""
            SELECT id AS "Id", branch_id AS "BranchId", category_id AS "CategoryId", name AS "Name",
                   description AS "Description", price AS "Price", stock_status AS "StockStatus", status AS "Status",
                   created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM menu_items WHERE id = {itemId.Value} AND branch_id = {branchId} AND deleted_at IS NULL FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (lockedBranchId is null || row is null)
        { await transaction.RollbackAsync(cancellationToken); return new(MenuStoreStatus.NotFound); }

        var current = MenuItem.Restore(NanoId.Create(row.Id), NanoId.Create(row.BranchId), NanoId.Create(row.CategoryId),
            row.Name, row.Description, row.Price, row.StockStatus, row.Status, row.CreatedAt, row.UpdatedAt);
        var target = available ? "available" : "out_of_stock";
        if (current.StockStatus == target)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(MenuStoreStatus.Updated, current);
        }
        var updated = current.SetStockStatus(target, now);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE menu_items SET stock_status = {updated.StockStatus}, updated_at = {updated.UpdatedAt}
            WHERE id = {itemId.Value} AND deleted_at IS NULL
            """, cancellationToken);
        var auditRows = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO audit_logs (id, actor_user_id, action, resource_type, resource_id, before_json, after_json, created_at)
            VALUES ({NanoId.New().Value}, {actorUserId.Value}, 'restaurant.menu.item.stock_status_changed', 'menu_item',
                {itemId.Value}, jsonb_build_object('stockStatus', {current.StockStatus}),
                jsonb_build_object('stockStatus', {updated.StockStatus}, 'status', {updated.Status}), {now})
            """, cancellationToken);
        if (auditRows != 1) throw new InvalidOperationException("The menu item stock audit event was not written.");
        await transaction.CommitAsync(cancellationToken);
        return new(MenuStoreStatus.Updated, updated);
    }

    private async Task<bool> IsVerifiedActiveUserAsync(NanoId userId, CancellationToken cancellationToken) =>
        await dbContext.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1 FROM users
                WHERE id = {userId.Value} AND status = 'active'
                  AND deleted_at IS NULL AND email_verified_at IS NOT NULL
            ) AS "Value"
            """).SingleAsync(cancellationToken);

    private async Task<bool> IsActiveRestaurantStaffAsync(
        string restaurantId, NanoId userId, CancellationToken cancellationToken) =>
        await dbContext.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1 FROM restaurant_staff
                WHERE restaurant_id = {restaurantId} AND user_id = {userId.Value}
                  AND status = 'active' AND role IN ('manager', 'staff')
            ) AS "Value"
            """).SingleAsync(cancellationToken);

    private Task<bool> HasRestaurantAccessAsync(
        string restaurantId, string ownerUserId, NanoId actorUserId, CancellationToken cancellationToken) =>
        string.Equals(ownerUserId, actorUserId.Value, StringComparison.Ordinal)
            ? Task.FromResult(true)
            : IsActiveRestaurantStaffAsync(restaurantId, actorUserId, cancellationToken);

    private static MenuItemDto Map(MenuItemRow row) => new(
        row.Id, row.BranchId, row.CategoryId, row.Name, row.Description, row.Price, row.StockStatus,
        row.Status, row.CreatedAt, row.UpdatedAt);

    private sealed class AuthorizedBranchRow
    {
        public string BranchId { get; init; } = "";
        public string RestaurantId { get; init; } = "";
        public string OwnerUserId { get; init; } = "";
    }

    private sealed class MenuCategoryRow
    {
        public string Id { get; init; } = "";
        public string BranchId { get; init; } = "";
        public string Name { get; init; } = "";
        public int DisplayOrder { get; init; }
        public string Status { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }

    private sealed class MenuItemRow
    {
        public string Id { get; init; } = "";
        public string BranchId { get; init; } = "";
        public string CategoryId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public string StockStatus { get; init; } = "";
        public string Status { get; init; } = "";
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
