using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261015120000_AlignPromotionAndPaymentTransactionConstraints")]
public sealed class AlignPromotionAndPaymentTransactionConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE promotions
                DROP CONSTRAINT promotions_check2;

            ALTER TABLE promotions
                ADD CONSTRAINT ck_promotions_max_discount_type
                CHECK (type IN ('percent', 'fixed') OR max_discount_amount IS NULL);

            CREATE UNIQUE INDEX ux_payment_transactions_provider_transaction_id
                ON payment_transactions (provider_transaction_id)
                WHERE provider_transaction_id IS NOT NULL;

            ALTER TABLE devices
                DROP CONSTRAINT devices_push_token_key;

            CREATE UNIQUE INDEX ux_devices_active_push_token
                ON devices (push_token)
                WHERE status = 'active';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM devices
                    GROUP BY push_token
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot roll back active push-token uniqueness while duplicate push_token rows exist';
                END IF;
            END $$;

            DROP INDEX IF EXISTS ux_payment_transactions_provider_transaction_id;
            DROP INDEX IF EXISTS ux_devices_active_push_token;

            ALTER TABLE promotions
                DROP CONSTRAINT IF EXISTS ck_promotions_max_discount_type;

            ALTER TABLE promotions
                ADD CONSTRAINT promotions_check2
                CHECK (type = 'percent' OR max_discount_amount IS NULL OR max_discount_amount >= 0);

            ALTER TABLE devices
                ADD CONSTRAINT devices_push_token_key UNIQUE (push_token);
            """);
    }
}
