using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingPlanningProfileDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_planning_profile_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    draft_json = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_planning_profile_drafts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_planning_profile_drafts_expires_at_utc",
                table: "pending_planning_profile_drafts",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_pending_planning_profile_drafts_user_id_conversation_id",
                table: "pending_planning_profile_drafts",
                columns: new[] { "user_id", "conversation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_planning_profile_drafts");
        }
    }
}
