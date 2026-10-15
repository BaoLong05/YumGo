using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityPhoneUniquenessForActiveUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_phone_not_deleted",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ux_users_phone_not_deleted",
                table: "users",
                column: "phone",
                unique: true,
                filter: "\"phone\" IS NOT NULL AND \"status\" = 'active' AND \"deleted_at\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_phone_not_deleted",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ux_users_phone_not_deleted",
                table: "users",
                column: "phone",
                unique: true,
                filter: "\"phone\" IS NOT NULL AND \"deleted_at\" IS NULL");
        }
    }
}
