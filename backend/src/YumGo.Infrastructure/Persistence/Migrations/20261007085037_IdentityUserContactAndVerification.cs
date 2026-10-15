using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityUserContactAndVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "users",
                type: "varchar(20)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_phone_not_deleted",
                table: "users",
                column: "phone",
                unique: true,
                filter: "\"phone\" IS NOT NULL AND \"deleted_at\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_email_normalized",
                table: "users",
                sql: "\"email\" = lower(\"email\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_phone_not_deleted",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_email_normalized",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "users");
        }
    }
}
