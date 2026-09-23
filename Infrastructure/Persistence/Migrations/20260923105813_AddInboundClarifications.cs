using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundClarifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clarification_question",
                table: "inbound_analyses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "needs_clarification",
                table: "inbound_analyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "inbound_clarifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_kind = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_display = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    original_text = table.Column<string>(type: "text", nullable: false),
                    original_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    question = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    shift_context = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deadline_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    answer_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email_message_id = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_inbound_clarifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inbound_clarifications_client_id_asked_at",
                table: "inbound_clarifications",
                columns: new[] { "client_id", "asked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inbound_clarifications_client_id_open",
                table: "inbound_clarifications",
                column: "client_id",
                unique: true,
                filter: "status = 0 AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_clarifications_original_analysis_id",
                table: "inbound_clarifications",
                column: "original_analysis_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_clarifications_result_analysis_id",
                table: "inbound_clarifications",
                column: "result_analysis_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_clarifications_status_deadline_at",
                table: "inbound_clarifications",
                columns: new[] { "status", "deadline_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_clarifications");

            migrationBuilder.DropColumn(
                name: "clarification_question",
                table: "inbound_analyses");

            migrationBuilder.DropColumn(
                name: "needs_clarification",
                table: "inbound_analyses");
        }
    }
}
