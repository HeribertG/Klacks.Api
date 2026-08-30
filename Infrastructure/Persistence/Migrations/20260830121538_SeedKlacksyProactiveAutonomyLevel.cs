using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Seeds the installation-wide proactive autonomy level with the fail-safe default 0 (Propose,
    /// report and wait) when the row does not exist yet. WHERE NOT EXISTS instead of ON CONFLICT and
    /// never DefaultSeed: ix_settings_type is unique, DefaultSeed's ON CONFLICT (id) cannot catch a
    /// type collision, and InsertData rows are skipped by the DatabaseInitializer - only a Sql(...)
    /// migration reaches existing databases.
    /// </summary>
    public partial class SeedKlacksyProactiveAutonomyLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO settings (id,type,value) " +
                "SELECT gen_random_uuid(),'KLACKSY_PROACTIVE_AUTONOMY_LEVEL','0' " +
                "WHERE NOT EXISTS (SELECT 1 FROM settings WHERE type='KLACKSY_PROACTIVE_AUTONOMY_LEVEL')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM settings WHERE type='KLACKSY_PROACTIVE_AUTONOMY_LEVEL'");
        }
    }
}
