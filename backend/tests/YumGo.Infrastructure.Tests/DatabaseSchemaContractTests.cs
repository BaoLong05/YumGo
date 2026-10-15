using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Tests;

public sealed class DatabaseSchemaContractTests
{
    [Fact]
    public async Task Free_delivery_promotion_cannot_have_max_discount_amount()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_promotion_contract_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO promotions (
                    id, code, name, type, value, min_order_amount, max_discount_amount,
                    per_user_limit, starts_at, ends_at, status, created_at, updated_at)
                VALUES (
                    'aaaaaaaaaaaaaaaaaaaaa', 'FREE', 'Free delivery', 'free_delivery', 0, 0, 100,
                    1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 day', 'active',
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                """));

        Assert.Equal("ck_promotions_max_discount_type", exception.ConstraintName);

        var negativeDiscountException = await Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO promotions (
                    id, code, name, type, value, min_order_amount, max_discount_amount,
                    per_user_limit, starts_at, ends_at, status, created_at, updated_at)
                VALUES (
                    'bbbbbbbbbbbbbbbbbbbbb', 'NEGATIVE', 'Negative cap', 'percent', 20, 0, -1,
                    1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 day', 'active',
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, negativeDiscountException.SqlState);
    }

    [Fact]
    public async Task Provider_transaction_id_must_be_unique_when_present()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_payment_transaction_contract_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO users (id, email, password_hash, status, created_at, updated_at)
            VALUES ('aaaaaaaaaaaaaaaaaaaaa', 'owner@example.test', 'hash', 'active', NOW(), NOW());

            INSERT INTO restaurants (id, owner_user_id, name, status, created_at, updated_at)
            VALUES ('bbbbbbbbbbbbbbbbbbbbb', 'aaaaaaaaaaaaaaaaaaaaa', 'Restaurant', 'active', NOW(), NOW());

            INSERT INTO restaurant_branches (
                id, restaurant_id, name, phone, address_line, ward, district, city,
                latitude, longitude, status, created_at, updated_at)
            VALUES (
                'ccccccccccccccccccccc', 'bbbbbbbbbbbbbbbbbbbbb', 'Main', 'branch-phone',
                '123 Example Street', 'Ward 1', 'District 1', 'HCMC', 10, 106, 'open', NOW(), NOW());

            INSERT INTO orders (
                id, order_number, customer_id, branch_id, status, subtotal, discount_amount,
                delivery_fee, tax_amount, total_amount, recipient_name_snapshot,
                recipient_phone_snapshot, address_line_snapshot, ward_snapshot, district_snapshot,
                city_snapshot, placed_at, created_at, updated_at)
            VALUES (
                'ddddddddddddddddddddd', 'order-1', 'aaaaaaaaaaaaaaaaaaaaa',
                'ccccccccccccccccccccc', 'pending', 100, 0, 0, 0, 100, 'Customer',
                'branch-phone', '123 Example Street', 'Ward 1', 'District 1', 'HCMC',
                NOW(), NOW(), NOW());

            INSERT INTO payments (
                id, order_id, method, status, amount, created_at, updated_at)
            VALUES (
                'eeeeeeeeeeeeeeeeeeeee', 'ddddddddddddddddddddd', 'card', 'pending', 100, NOW(), NOW());

            INSERT INTO payment_transactions (
                id, payment_id, provider_event_id, provider_transaction_id,
                transaction_type, amount, status, created_at)
            VALUES (
                'fffffffffffffffffffff', 'eeeeeeeeeeeeeeeeeeeee', 'event-1', 'provider-tx-1',
                'capture', 100, 'received', NOW());
            """);

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO payment_transactions (
                id, payment_id, provider_event_id, provider_transaction_id,
                transaction_type, amount, status, created_at)
            VALUES
                ('hhhhhhhhhhhhhhhhhhhhh', 'eeeeeeeeeeeeeeeeeeeee', 'event-no-tx-1', NULL, 'failure', 100, 'received', NOW()),
                ('iiiiiiiiiiiiiiiiiiiii', 'eeeeeeeeeeeeeeeeeeeee', 'event-no-tx-2', NULL, 'failure', 100, 'received', NOW());
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO payment_transactions (
                    id, payment_id, provider_event_id, provider_transaction_id,
                    transaction_type, amount, status, created_at)
                VALUES (
                    'ggggggggggggggggggggg', 'eeeeeeeeeeeeeeeeeeeee', 'event-2', 'provider-tx-1',
                    'capture', 100, 'received', NOW())
                """));

        Assert.Equal("ux_payment_transactions_provider_transaction_id", exception.ConstraintName);
    }

    [Fact]
    public async Task Inactive_device_token_can_be_reused_by_one_active_device()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_device_token_contract_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO users (id, email, password_hash, status, created_at, updated_at)
            VALUES ('aaaaaaaaaaaaaaaaaaaaa', 'device-user@example.test', 'hash', 'active', NOW(), NOW());

            INSERT INTO devices (id, user_id, platform, push_token, status, created_at, updated_at)
            VALUES ('bbbbbbbbbbbbbbbbbbbbb', 'aaaaaaaaaaaaaaaaaaaaa', 'ios', 'device-token', 'inactive', NOW(), NOW());

            INSERT INTO devices (id, user_id, platform, push_token, status, created_at, updated_at)
            VALUES ('ccccccccccccccccccccc', 'aaaaaaaaaaaaaaaaaaaaa', 'ios', 'device-token', 'active', NOW(), NOW());
            """);

        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO devices (id, user_id, platform, push_token, status, created_at, updated_at)
                VALUES ('ddddddddddddddddddddd', 'aaaaaaaaaaaaaaaaaaaaa', 'ios', 'device-token', 'active', NOW(), NOW());
                """));
        Assert.Equal("ux_devices_active_push_token", exception.ConstraintName);

        var activeRows = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM devices WHERE push_token = 'device-token' AND status = 'active'")
            .SingleAsync();

        Assert.Equal(1, activeRows);
    }

    [Fact]
    public async Task Rolling_back_schema_contract_migration_restores_previous_constraints()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_schema_contract_rollback_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        await context.GetService<IMigrator>().MigrateAsync("20261015035318_SeedCustomerRole");

        var activeTokenIndexCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_indexes WHERE indexname = 'ux_devices_active_push_token'")
            .SingleAsync();
        var globalTokenConstraintCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_constraint WHERE conname = 'devices_push_token_key' AND contype = 'u'")
            .SingleAsync();
        var revisedPromotionConstraintCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_constraint WHERE conname = 'ck_promotions_max_discount_type'")
            .SingleAsync();
        var previousPromotionConstraintCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_constraint WHERE conname = 'promotions_check2'")
            .SingleAsync();

        Assert.Equal(0, activeTokenIndexCount);
        Assert.Equal(1, globalTokenConstraintCount);
        Assert.Equal(0, revisedPromotionConstraintCount);
        Assert.Equal(1, previousPromotionConstraintCount);
    }

    [Fact]
    public async Task Rolling_back_with_duplicate_inactive_device_tokens_fails_without_losing_data()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("yumgo_schema_contract_rollback_data_test")
            .WithUsername("postgres")
            .WithPassword(Guid.NewGuid().ToString("N"))
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO users (id, email, password_hash, status, created_at, updated_at)
            VALUES ('aaaaaaaaaaaaaaaaaaaaa', 'device-owner@example.test', 'hash', 'active', NOW(), NOW());

            INSERT INTO devices (id, user_id, platform, push_token, status, created_at, updated_at)
            VALUES
                ('bbbbbbbbbbbbbbbbbbbbb', 'aaaaaaaaaaaaaaaaaaaaa', 'ios', 'reused-token', 'inactive', NOW(), NOW()),
                ('ccccccccccccccccccccc', 'aaaaaaaaaaaaaaaaaaaaa', 'ios', 'reused-token', 'inactive', NOW(), NOW());
            """);

        var rollbackException = await Assert.ThrowsAsync<PostgresException>(async () =>
            await context.GetService<IMigrator>().MigrateAsync("20261015035318_SeedCustomerRole"));

        Assert.Contains("Cannot roll back active push-token uniqueness", rollbackException.Message);

        var deviceCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM devices WHERE push_token = 'reused-token'")
            .SingleAsync();
        var activeTokenIndexCount = await context.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_indexes WHERE indexname = 'ux_devices_active_push_token'")
            .SingleAsync();

        Assert.Equal(2, deviceCount);
        Assert.Equal(1, activeTokenIndexCount);
    }
}
