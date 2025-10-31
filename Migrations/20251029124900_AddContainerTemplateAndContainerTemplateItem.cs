using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerTemplateAndContainerTemplateItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_container",
                table: "shift");

            migrationBuilder.CreateTable(
                name: "container_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    is_weekday_or_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_holiday = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_container_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_template_shift_container_id",
                        column: x => x.container_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "container_template_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_container_template_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_template_item_container_template_container_templa",
                        column: x => x.container_template_id,
                        principalTable: "container_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_container_template_item_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_container_template_container_id",
                table: "container_template",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_id_container_id_weekday_is_weekday_or_ho",
                table: "container_template",
                columns: new[] { "id", "container_id", "weekday", "is_weekday_or_holiday", "is_holiday" });

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_container_template_id",
                table: "container_template_item",
                column: "container_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_shift_id",
                table: "container_template_item",
                column: "shift_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "container_template_item");

            migrationBuilder.DropTable(
                name: "container_template");

            migrationBuilder.AddColumn<bool>(
                name: "is_container",
                table: "shift",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
