using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroImportKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "import_content_hash",
                table: "macro",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "import_source_key",
                table: "macro",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_macro_import_source_key",
                table: "macro",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_macro_import_source_key",
                table: "macro");

            migrationBuilder.DropColumn(
                name: "import_content_hash",
                table: "macro");

            migrationBuilder.DropColumn(
                name: "import_source_key",
                table: "macro");
        }
    }
}
