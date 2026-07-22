using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    recipe_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    step_index = table.Column<int>(type: "integer", nullable: false),
                    slots_json = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    awaiting_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    capture_rewind_used = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_recipes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_recipes_expires_at_utc",
                table: "pending_recipes",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_pending_recipes_user_id_conversation_id",
                table: "pending_recipes",
                columns: new[] { "user_id", "conversation_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_recipes");
        }
    }
}
