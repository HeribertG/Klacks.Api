using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerShiftOverrideTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "container_shift_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    from_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    start_base = table.Column<string>(type: "text", nullable: true),
                    end_base = table.Column<string>(type: "text", nullable: true),
                    route_info = table.Column<string>(type: "jsonb", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_shift_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_shift_overrides_shift_container_id",
                        column: x => x.container_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "container_shift_override_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_shift_override_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    briefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    debriefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_after = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_before = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_range_start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_shift_override_items", x => x.id);
                    table.CheckConstraint("CK_ContainerShiftOverrideItem_ShiftXorAbsence", "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_container_shift_overrides_co",
                        column: x => x.container_shift_override_id,
                        principalTable: "container_shift_overrides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_absence_id",
                table: "container_shift_override_items",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_container_shift_override_id",
                table: "container_shift_override_items",
                column: "container_shift_override_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_shift_id",
                table: "container_shift_override_items",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_overrides_container_id_date",
                table: "container_shift_overrides",
                columns: new[] { "container_id", "date" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "container_shift_override_items");

            migrationBuilder.DropTable(
                name: "container_shift_overrides");
        }
    }
}
