using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261016133500_RestaurantWorkflowLifecycle")]
public sealed class RestaurantWorkflowLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE restaurants DROP CONSTRAINT restaurants_status_check;
            ALTER TABLE restaurants ADD CONSTRAINT restaurants_status_check
                CHECK (status IN ('pending', 'active', 'rejected', 'suspended', 'closed'));

            INSERT INTO roles (id, code, name, created_at)
            VALUES ('admin_role_seed_00001', 'admin', 'Administrator', CURRENT_TIMESTAMP)
            ON CONFLICT (code) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM restaurants WHERE status = 'rejected') THEN
                    RAISE EXCEPTION 'Cannot roll back restaurant review state while rejected restaurants exist.';
                END IF;
            END $$;

            ALTER TABLE restaurants DROP CONSTRAINT restaurants_status_check;
            ALTER TABLE restaurants ADD CONSTRAINT restaurants_status_check
                CHECK (status IN ('pending', 'active', 'suspended', 'closed'));

            """);
    }
}
