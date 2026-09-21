using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_standing_approval",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    daily_budget = table.Column<int>(type: "integer", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_agent_standing_approval", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_standing_approval_trigger_kind_group",
                table: "agent_standing_approval",
                columns: new[] { "trigger_kind", "group_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_standing_approval");
        }
    }
}
