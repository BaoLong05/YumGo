using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using YumGo.Application.Addresses;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Addresses;

public sealed class PostgresGoogleGeocodingQuotaStore(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : IGoogleGeocodingQuota
{
    private const int MaximumFreeTierSafetyCap = 9000;

    public async Task<bool> TryReserveAsync(int monthlyLimit, CancellationToken cancellationToken)
    {
        if (monthlyLimit is < 1 or > MaximumFreeTierSafetyCap)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyLimit));
        }

        var now = timeProvider.GetUtcNow();
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        try
        {
            if (closeConnection) await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO google_geocoding_usage (month_start, request_count, updated_at)
                VALUES (@month_start, 1, @updated_at)
                ON CONFLICT (month_start) DO UPDATE
                    SET request_count = google_geocoding_usage.request_count + 1,
                        updated_at = EXCLUDED.updated_at
                    WHERE google_geocoding_usage.request_count < @monthly_limit
                RETURNING 1;
                """;
            if (dbContext.Database.CurrentTransaction is { } transaction)
                command.Transaction = transaction.GetDbTransaction();
            AddParameter(command, "month_start", monthStart);
            AddParameter(command, "updated_at", now);
            AddParameter(command, "monthly_limit", monthlyLimit);
            return await command.ExecuteScalarAsync(cancellationToken) is not null;
        }
        catch (DbException exception)
        {
            throw new GeocodingUnavailableException("Could not reserve Google Geocoding quota.", exception);
        }
        finally
        {
            if (closeConnection) await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
