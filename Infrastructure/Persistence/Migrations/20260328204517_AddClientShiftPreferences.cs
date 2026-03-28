using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientShiftPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "client_shift_preference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preference_type = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_client_shift_preference", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_shift_preference_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_shift_preference_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_client_id_is_deleted",
                table: "client_shift_preference",
                columns: new[] { "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_client_id_shift_id_preference_type",
                table: "client_shift_preference",
                columns: new[] { "client_id", "shift_id", "preference_type" });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_shift_id",
                table: "client_shift_preference",
                column: "shift_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_shift_preference");
        }
    }
}
