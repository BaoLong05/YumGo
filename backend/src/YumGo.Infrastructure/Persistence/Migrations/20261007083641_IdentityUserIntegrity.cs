using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityUserIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ux_users_email_active",
                table: "users",
                newName: "ux_users_email_not_deleted");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_id_nanoid_21",
                table: "users",
                sql: "\"id\" ~ '^[A-Za-z0-9_-]{21}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_users_id_nanoid_21",
                table: "users");

            migrationBuilder.RenameIndex(
                name: "ux_users_email_not_deleted",
                table: "users",
                newName: "ux_users_email_active");
        }
    }
}
