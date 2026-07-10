using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportFormatOverrideAndLogFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "override_applied",
                table: "export_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "export_format_override",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    patch_json = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_under_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_format_override", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_format_override_format_key",
                table: "export_format_override",
                column: "format_key",
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_format_override");

            migrationBuilder.DropColumn(
                name: "override_applied",
                table: "export_log");
        }
    }
}
