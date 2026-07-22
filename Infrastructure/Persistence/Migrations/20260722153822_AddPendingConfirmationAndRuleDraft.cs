using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingConfirmationAndRuleDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_company_rule_drafts",
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
                    table.PrimaryKey("pk_pending_company_rule_drafts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pending_confirmations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_confirmations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_company_rule_drafts_expires_at_utc",
                table: "pending_company_rule_drafts",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_pending_company_rule_drafts_user_id_conversation_id",
                table: "pending_company_rule_drafts",
                columns: new[] { "user_id", "conversation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_confirmations_expires_at_utc",
                table: "pending_confirmations",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_pending_confirmations_token",
                table: "pending_confirmations",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_confirmations_user_id",
                table: "pending_confirmations",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_company_rule_drafts");

            migrationBuilder.DropTable(
                name: "pending_confirmations");
        }
    }
}
