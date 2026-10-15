using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Tests;

public sealed class FullSchemaMigrationTests
{
    [Fact]
    public void Migration_script_creates_every_table_in_the_database_specification()
    {
        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql("Host=localhost;Database=yumgo_test;Username=yumgo;Password=unused")
                .Options);

        var script = context.GetService<IMigrator>().GenerateScript();
        var tableNames = new[]
        {
            "users", "roles", "permissions", "user_roles", "role_permissions", "refresh_tokens",
            "customer_profiles", "addresses", "restaurants", "restaurant_branches", "restaurant_staff",
            "driver_profiles", "driver_availability", "menu_categories", "menu_items", "carts", "cart_items",
            "promotions", "orders", "order_items", "payments", "payment_transactions", "refunds", "deliveries",
            "delivery_assignments", "driver_locations", "reviews", "devices", "notifications", "idempotency_keys",
            "outbox_messages", "audit_logs"
        };

        Assert.Equal(32, tableNames.Length);
        foreach (var tableName in tableNames)
        {
            Assert.Contains($"CREATE TABLE {tableName}", script, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("order_number varchar(40) NOT NULL UNIQUE", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("A-Za-z0-9_-", script, StringComparison.Ordinal);
        Assert.Contains("ix_delivery_assignments_delivery", script, StringComparison.OrdinalIgnoreCase);
    }
}
