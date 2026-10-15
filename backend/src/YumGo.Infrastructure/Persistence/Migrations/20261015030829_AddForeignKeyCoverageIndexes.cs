using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumGo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyCoverageIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_branches_restaurant_id_fk",
                table: "restaurant_branches",
                column: "restaurant_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_branch_id_fk",
                table: "menu_items",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_category_id_fk",
                table: "menu_items",
                column: "category_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_branches_restaurant_id_fk",
                table: "restaurant_branches");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_branch_id_fk",
                table: "menu_items");

            migrationBuilder.DropIndex(
                name: "ix_menu_items_category_id_fk",
                table: "menu_items");
        }
    }
}
