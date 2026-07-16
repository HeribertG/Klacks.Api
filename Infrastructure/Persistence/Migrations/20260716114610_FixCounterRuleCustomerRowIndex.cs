using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixCounterRuleCustomerRowIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_counter_rule_import_source_key;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_counter_rule_import_source_key ON counter_rule (import_source_key) WHERE is_deleted = false AND import_source_key <> '';");
            migrationBuilder.Sql("ALTER TABLE counter_rule ALTER COLUMN import_source_key SET DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE counter_rule ALTER COLUMN import_content_hash SET DEFAULT '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE counter_rule ALTER COLUMN import_content_hash DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE counter_rule ALTER COLUMN import_source_key DROP DEFAULT;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_counter_rule_import_source_key;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_counter_rule_import_source_key ON counter_rule (import_source_key) WHERE is_deleted = false;");
        }
    }
}
