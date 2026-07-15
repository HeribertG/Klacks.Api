using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportKeysToSchedulingRuleAndQualification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "import_content_hash",
                table: "scheduling_rules",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "import_source_key",
                table: "scheduling_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "import_content_hash",
                table: "qualification",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "import_source_key",
                table: "qualification",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rules_import_source_key",
                table: "scheduling_rules",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_qualification_import_source_key",
                table: "qualification",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scheduling_rules_import_source_key",
                table: "scheduling_rules");

            migrationBuilder.DropIndex(
                name: "ix_qualification_import_source_key",
                table: "qualification");

            migrationBuilder.DropColumn(
                name: "import_content_hash",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "import_source_key",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "import_content_hash",
                table: "qualification");

            migrationBuilder.DropColumn(
                name: "import_source_key",
                table: "qualification");
        }
    }
}
