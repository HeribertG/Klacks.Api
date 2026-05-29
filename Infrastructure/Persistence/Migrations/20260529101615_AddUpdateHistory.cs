using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "update_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    from_version = table.Column<string>(type: "text", nullable: false),
                    target_version = table.Column<string>(type: "text", nullable: false),
                    artifact_ref = table.Column<string>(type: "text", nullable: true),
                    artifact_sha256 = table.Column<string>(type: "text", nullable: true),
                    artifact_signature = table.Column<string>(type: "text", nullable: true),
                    contains_migrations = table.Column<bool>(type: "boolean", nullable: false),
                    backup_ref = table.Column<string>(type: "text", nullable: true),
                    related_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by = table.Column<string>(type: "text", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_update_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_update_history_requested_at",
                table: "update_history",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "ix_update_history_status",
                table: "update_history",
                column: "status",
                filter: "is_deleted = false");

            // At most one active (Pending=0 / Running=1) operation may exist at a time.
            // Indexes a constant expression that EF cannot model, so it is created via raw SQL.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_update_history_single_active ON update_history ((true)) " +
                "WHERE status IN (0, 1) AND is_deleted = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "update_history");
        }
    }
}
