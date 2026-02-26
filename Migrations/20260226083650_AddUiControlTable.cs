using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUiControlTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ui_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_key = table.Column<string>(type: "text", nullable: false),
                    control_key = table.Column<string>(type: "text", nullable: false),
                    selector = table.Column<string>(type: "text", nullable: false),
                    selector_type = table.Column<string>(type: "text", nullable: false),
                    control_type = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    route = table.Column<string>(type: "text", nullable: true),
                    parent_control_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_dynamic = table.Column<bool>(type: "boolean", nullable: false),
                    selector_pattern = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_ui_controls", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_controls_ui_controls_parent_control_id",
                        column: x => x.parent_control_id,
                        principalTable: "ui_controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_page_key_control_key",
                table: "ui_controls",
                columns: new[] { "page_key", "control_key" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_page_key_sort_order",
                table: "ui_controls",
                columns: new[] { "page_key", "sort_order" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_parent_control_id",
                table: "ui_controls",
                column: "parent_control_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_controls");
        }
    }
}
