using Microsoft.EntityFrameworkCore;
using YumGo.Application.Addresses;
using YumGo.Domain.Addresses;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Addresses;

public sealed class EfCustomerAddressStore(ApplicationDbContext dbContext) : ICustomerAddressStore
{
    public async Task<IReadOnlyList<CustomerAddress>> ListAsync(NanoId userId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database.SqlQuery<CustomerAddressRow>($"""
            SELECT id AS "Id", user_id AS "UserId", label AS "Label", recipient_name AS "RecipientName",
                   recipient_phone AS "RecipientPhone", address_line AS "AddressLine", ward AS "Ward",
                   district AS "District", city AS "City", latitude AS "Latitude", longitude AS "Longitude",
                   is_default AS "IsDefault", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM addresses
            WHERE user_id = {userId.Value}
            ORDER BY is_default DESC, updated_at DESC, id
            """).ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<CustomerAddress?> FindAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Database.SqlQuery<CustomerAddressRow>($"""
            SELECT id AS "Id", user_id AS "UserId", label AS "Label", recipient_name AS "RecipientName",
                   recipient_phone AS "RecipientPhone", address_line AS "AddressLine", ward AS "Ward",
                   district AS "District", city AS "City", latitude AS "Latitude", longitude AS "Longitude",
                   is_default AS "IsDefault", created_at AS "CreatedAt", updated_at AS "UpdatedAt"
            FROM addresses
            WHERE id = {addressId.Value} AND user_id = {userId.Value}
            """).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task CreateAsync(CustomerAddress address, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (address.IsDefault)
        {
            await LockUserAsync(address.UserId, cancellationToken);
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE addresses SET is_default = false, updated_at = {address.UpdatedAt}
                WHERE user_id = {address.UserId.Value} AND is_default = true
                """, cancellationToken);
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO addresses (
                id, user_id, label, recipient_name, recipient_phone, address_line, ward, district, city,
                latitude, longitude, is_default, created_at, updated_at)
            VALUES (
                {address.Id.Value}, {address.UserId.Value}, {address.Label}, {address.RecipientName},
                {address.RecipientPhone.Value}, {address.AddressLine}, {address.Ward}, {address.District},
                {address.City}, {address.Latitude}, {address.Longitude}, {address.IsDefault},
                {address.CreatedAt}, {address.UpdatedAt})
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(CustomerAddress address, bool? isDefaultOverride, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (isDefaultOverride.HasValue)
        {
            await LockUserAsync(address.UserId, cancellationToken);
            if (isDefaultOverride.Value)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE addresses SET is_default = false, updated_at = {address.UpdatedAt}
                    WHERE user_id = {address.UserId.Value} AND id <> {address.Id.Value} AND is_default = true
                    """, cancellationToken);
            }
        }

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE addresses
            SET label = {address.Label}, recipient_name = {address.RecipientName},
                recipient_phone = {address.RecipientPhone.Value}, address_line = {address.AddressLine},
                ward = {address.Ward}, district = {address.District}, city = {address.City},
                latitude = {address.Latitude}, longitude = {address.Longitude},
                is_default = COALESCE({isDefaultOverride}, is_default), updated_at = {address.UpdatedAt}
            WHERE id = {address.Id.Value} AND user_id = {address.UserId.Value}
            """, cancellationToken);
        if (affected != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(NanoId userId, NanoId addressId, CancellationToken cancellationToken)
    {
        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM addresses WHERE id = {addressId.Value} AND user_id = {userId.Value}
            """, cancellationToken);
        return affected == 1;
    }

    public async Task<bool> SetDefaultAsync(NanoId userId, NanoId addressId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LockUserAsync(userId, cancellationToken);
        var lockedAddressId = await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value" FROM addresses
            WHERE id = {addressId.Value} AND user_id = {userId.Value}
            FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
        if (lockedAddressId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE addresses SET is_default = false, updated_at = {now}
            WHERE user_id = {userId.Value} AND is_default = true
            """, cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE addresses SET is_default = true, updated_at = {now}
            WHERE id = {addressId.Value} AND user_id = {userId.Value}
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task LockUserAsync(NanoId userId, CancellationToken cancellationToken)
    {
        await dbContext.Database.SqlQuery<string>($"""
            SELECT id AS "Value" FROM users WHERE id = {userId.Value} FOR UPDATE
            """).SingleOrDefaultAsync(cancellationToken);
    }

    private static CustomerAddress Map(CustomerAddressRow row) => CustomerAddress.Create(
        NanoId.Create(row.Id), NanoId.Create(row.UserId), row.Label, row.RecipientName,
        PhoneNumber.Create(row.RecipientPhone), row.AddressLine, row.Ward, row.District, row.City,
        row.Latitude, row.Longitude, row.IsDefault, row.CreatedAt, row.UpdatedAt);

    private sealed class CustomerAddressRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
