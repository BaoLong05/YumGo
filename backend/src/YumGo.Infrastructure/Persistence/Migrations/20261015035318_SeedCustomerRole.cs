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
            // Keep this reference row: Up may have reused a pre-existing customer role.
        }
    }
}
