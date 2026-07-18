using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodCapRuleAdminCrudIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_period_cap_rule_import_source_key",
                table: "period_cap_rule");

            migrationBuilder.AlterColumn<string>(
                name: "import_source_key",
                table: "period_cap_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.Sql(@"
                UPDATE period_cap_rule
                SET is_deleted = true, deleted_time = NOW()
                WHERE is_deleted = false
                  AND import_source_key <> ''
                  AND id NOT IN (
                      SELECT DISTINCT ON (import_source_key) id
                      FROM period_cap_rule
                      WHERE is_deleted = false AND import_source_key <> ''
                      ORDER BY import_source_key, create_time NULLS FIRST, id
                  );");

            migrationBuilder.CreateIndex(
                name: "ix_period_cap_rule_import_source_key",
                table: "period_cap_rule",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_period_cap_rule_import_source_key",
                table: "period_cap_rule");

            migrationBuilder.AlterColumn<string>(
                name: "import_source_key",
                table: "period_cap_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_period_cap_rule_import_source_key",
                table: "period_cap_rule",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false");
        }
    }
}
