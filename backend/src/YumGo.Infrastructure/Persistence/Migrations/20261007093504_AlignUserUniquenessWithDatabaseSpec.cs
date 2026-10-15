using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations;

public partial class AlignUserUniquenessWithDatabaseSpec : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_users_email_not_deleted", table: "users");
        migrationBuilder.DropIndex(name: "ux_users_phone_not_deleted", table: "users");

        migrationBuilder.CreateIndex(
            name: "ux_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_users_phone",
            table: "users",
            column: "phone",
            unique: true,
            filter: "\"phone\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ux_users_email", table: "users");
        migrationBuilder.DropIndex(name: "ux_users_phone", table: "users");

        migrationBuilder.CreateIndex(
            name: "ux_users_email_not_deleted",
            table: "users",
            column: "email",
            unique: true,
            filter: "\"deleted_at\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "ux_users_phone_not_deleted",
            table: "users",
            column: "phone",
            unique: true,
            filter: "\"phone\" IS NOT NULL AND \"status\" = 'active' AND \"deleted_at\" IS NULL");
    }
}
