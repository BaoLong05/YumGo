using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YumGo.Infrastructure.Persistence;

namespace YumGo.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261016031500_GoogleGeocodingMonthlyQuota")]
public sealed class GoogleGeocodingMonthlyQuota : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE google_geocoding_usage (
                month_start date PRIMARY KEY CHECK (EXTRACT(DAY FROM month_start) = 1),
                request_count integer NOT NULL CHECK (request_count BETWEEN 0 AND 9000),
                updated_at timestamptz NOT NULL
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE google_geocoding_usage;");
    }
}
