using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropFloorPlanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "floor_plan_work_marker");

            migrationBuilder.DropTable(
                name: "floor_plan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "floor_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    canvas_json = table.Column<string>(type: "text", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    thumbnail_data = table.Column<string>(type: "text", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_floor_plan", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "floor_plan_work_marker",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    floor_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<string>(type: "text", nullable: true),
                    from_date = table.Column<DateOnly>(type: "date", nullable: true),
                    height = table.Column<double>(type: "double precision", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    marker_type = table.Column<int>(type: "integer", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_time = table.Column<string>(type: "text", nullable: true),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    width = table.Column<double>(type: "double precision", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: true),
                    x = table.Column<double>(type: "double precision", nullable: false),
                    y = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_floor_plan_work_marker", x => x.id);
                    table.ForeignKey(
                        name: "fk_floor_plan_work_marker_floor_plan_floor_plan_id",
                        column: x => x.floor_plan_id,
                        principalTable: "floor_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_floor_plan_work_marker_floor_plan_id",
                table: "floor_plan_work_marker",
                column: "floor_plan_id");
        }
    }
}
