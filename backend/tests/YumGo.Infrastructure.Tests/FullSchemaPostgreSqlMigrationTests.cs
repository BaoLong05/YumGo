using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Tests;

public sealed class FullSchemaPostgreSqlMigrationTests
{
    [Fact]
    public async Task All_migrations_apply_to_clean_postgresql_and_enforce_schema_constraints()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_migration_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261007090000_FullCommerceSchema", appliedMigrations);
        Assert.Contains("20261007093504_AlignUserUniquenessWithDatabaseSpec", appliedMigrations);

        var applicationTableCount = await context.Database
            .SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name <> '__EFMigrationsHistory'
                """)
            .SingleAsync();
        Assert.Equal(32, applicationTableCount);

        var correctlySizedIdColumns = await context.Database
            .SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND column_name = 'id'
                  AND data_type = 'character varying'
                  AND character_maximum_length = 21
                """)
            .SingleAsync();
        Assert.Equal(32, correctlySizedIdColumns);

        var foreignKeyCount = await context.Database
            .SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value"
                FROM pg_constraint
                WHERE contype = 'f'
                  AND connamespace = 'public'::regnamespace
                """)
            .SingleAsync();
        Assert.Equal(43, foreignKeyCount);

        var foreignKeysWithoutLeadingIndex = await context.Database
            .SqlQueryRaw<string>("""
                SELECT fk.conrelid::regclass::text || '.' || fk.conname AS "Value"
                FROM pg_constraint fk
                WHERE fk.contype = 'f'
                  AND fk.connamespace = 'public'::regnamespace
                  AND NOT EXISTS (
                      SELECT 1
                      FROM pg_index idx
                      JOIN pg_class index_relation ON index_relation.oid = idx.indexrelid
                      JOIN pg_am access_method
                        ON access_method.oid = index_relation.relam
                       AND access_method.amname = 'btree'
                      WHERE idx.indrelid = fk.conrelid
                        AND idx.indisvalid
                        AND idx.indisready
                        AND idx.indpred IS NULL
                        AND idx.indnkeyatts >= cardinality(fk.conkey)
                        AND (
                            SELECT array_agg(key_column.attnum ORDER BY key_column.attnum)
                            FROM unnest(idx.indkey) WITH ORDINALITY AS key_column(attnum, ordinality)
                            WHERE key_column.ordinality <= cardinality(fk.conkey)
                        ) = (
                            SELECT array_agg(fk_column.attnum ORDER BY fk_column.attnum)
                            FROM unnest(fk.conkey) AS fk_column(attnum)
                        )
                  )
                """)
            .ToListAsync();
        Assert.True(
            foreignKeysWithoutLeadingIndex.Count == 0,
            $"Foreign keys without a valid non-partial index whose first key columns cover all FK columns: {string.Join(", ", foreignKeysWithoutLeadingIndex)}");

        var checkConstraintCount = await context.Database
            .SqlQueryRaw<int>("""
                SELECT count(*)::int AS "Value"
                FROM pg_constraint
                WHERE contype = 'c'
                  AND connamespace = 'public'::regnamespace
                """)
            .SingleAsync();
        Assert.Equal(132, checkConstraintCount);

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (id, email, password_hash, status, created_at, updated_at)
            VALUES ('invalid-id', 'invalid-id@example.test', 'encoded-hash', 'active', NOW(), NOW())
            """;

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("ck_users_id_nanoid_21", exception.ConstraintName);
    }
}
