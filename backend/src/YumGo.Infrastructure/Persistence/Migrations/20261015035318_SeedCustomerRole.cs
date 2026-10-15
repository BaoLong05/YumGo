using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCustomerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO roles (id, code, name, created_at)
                VALUES ('customer_role_seed_01', 'customer', 'Customer', CURRENT_TIMESTAMP)
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM roles
                WHERE id = 'customer_role_seed_01'
                  AND code = 'customer'
                  AND NOT EXISTS (
                      SELECT 1 FROM user_roles WHERE role_id = 'customer_role_seed_01'
                  );
                """);
        }
    }
}
