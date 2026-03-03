using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plugin_docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plugin_code = table.Column<string>(type: "text", nullable: false),
                    manual_name = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plugin_docs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plugin_docs_plugin_code_manual_name",
                table: "plugin_docs",
                columns: new[] { "plugin_code", "manual_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plugin_docs");
        }
    }
}
