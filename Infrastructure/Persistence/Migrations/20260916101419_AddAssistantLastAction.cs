using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantLastAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistant_last_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    calls_json = table.Column<string>(type: "text", nullable: false),
                    assistant_answer_excerpt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    clarification_skills_json = table.Column<string>(type: "text", nullable: false),
                    create_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    superseded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assistant_last_actions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assistant_last_actions_expires_at_utc",
                table: "assistant_last_actions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_assistant_last_actions_user_id_conversation_id",
                table: "assistant_last_actions",
                columns: new[] { "user_id", "conversation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_last_actions");
        }
    }
}
