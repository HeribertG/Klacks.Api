using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSealedDayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sealed_day",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sealed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("pk_sealed_day", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day",
                column: "date",
                unique: true,
                filter: "\"group_id\" IS NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day",
                columns: new[] { "date", "group_id" },
                unique: true,
                filter: "\"group_id\" IS NOT NULL AND \"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sealed_day");
        }
    }
}
