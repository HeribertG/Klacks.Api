using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSynonymsJsonbToAgentSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "synonyms",
                table: "agent_skills",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "skill_synonyms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_skill_synonyms", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_synonyms_language",
                table: "skill_synonyms",
                column: "language");

            migrationBuilder.CreateIndex(
                name: "ix_skill_synonyms_skill_name_language_keyword",
                table: "skill_synonyms",
                columns: new[] { "skill_name", "language", "keyword" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_synonyms");

            migrationBuilder.DropColumn(
                name: "synonyms",
                table: "agent_skills");
        }
    }
}
