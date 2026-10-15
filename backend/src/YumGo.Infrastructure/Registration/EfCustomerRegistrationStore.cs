using Microsoft.EntityFrameworkCore;
using Npgsql;
using YumGo.Application.Identity;
using YumGo.Domain.Common;
using YumGo.Domain.Identity;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Registration;

public sealed class EfCustomerRegistrationStore(ApplicationDbContext dbContext) : ICustomerRegistrationStore
{
    public async Task<CustomerRegistrationStoreStatus> CreateAsync(
        User user,
        CustomerProfile profile,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Set<User>().Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var duplicateStatus = GetDuplicateStatus(exception);
            if (duplicateStatus is null)
            {
                throw;
            }

            await transaction.RollbackAsync(cancellationToken);
            return duplicateStatus.Value;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO customer_profiles (id, user_id, full_name, created_at, updated_at)
            VALUES ({profile.Id.Value}, {profile.UserId.Value}, {profile.FullName}, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            """, cancellationToken);

        var roleAssignmentCount = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_roles (id, user_id, role_id, created_at)
            SELECT {NanoId.New().Value}, {user.Id.Value}, roles.id, CURRENT_TIMESTAMP
            FROM roles
            WHERE roles.code = 'customer'
            """, cancellationToken);

        if (roleAssignmentCount != 1)
        {
            throw new InvalidOperationException("The customer role is not seeded or is not unique.");
        }

        await transaction.CommitAsync(cancellationToken);
        return CustomerRegistrationStoreStatus.Created;
    }

    private static CustomerRegistrationStoreStatus? GetDuplicateStatus(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is not PostgresException postgresException
                || postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
            {
                continue;
            }

            return postgresException.ConstraintName switch
            {
                "ux_users_email" => CustomerRegistrationStoreStatus.DuplicateEmail,
                "ux_users_phone" => CustomerRegistrationStoreStatus.DuplicatePhone,
                _ => null
            };
        }

        return null;
    }
}
