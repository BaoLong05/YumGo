using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261015140000_EnforceMenuItemCategoryBranchScope")]
public sealed class EnforceMenuItemCategoryBranchScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_menu_categories_branch_id_id
                ON menu_categories (branch_id, id);

            CREATE INDEX ix_menu_items_branch_category_fk
                ON menu_items (branch_id, category_id);

            ALTER TABLE menu_items
                DROP CONSTRAINT menu_items_category_id_fkey;

            ALTER TABLE menu_items
                ADD CONSTRAINT fk_menu_items_category_branch
                FOREIGN KEY (branch_id, category_id)
                REFERENCES menu_categories (branch_id, id)
                ON DELETE RESTRICT;

            DROP INDEX ix_menu_items_branch_id_fk;
            DROP INDEX ix_menu_items_category_id_fk;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE menu_items
                DROP CONSTRAINT fk_menu_items_category_branch;

            ALTER TABLE menu_items
                ADD CONSTRAINT menu_items_category_id_fkey
                FOREIGN KEY (category_id)
                REFERENCES menu_categories (id)
                ON DELETE RESTRICT;

            DROP INDEX ix_menu_items_branch_category_fk;
            DROP INDEX ux_menu_categories_branch_id_id;

            CREATE INDEX ix_menu_items_branch_id_fk
                ON menu_items (branch_id);

            CREATE INDEX ix_menu_items_category_id_fk
                ON menu_items (category_id);
            """);
    }
}
