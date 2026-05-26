using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropAgentTemplatesAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_agents_agent_templates_template_id",
                table: "agents");

            migrationBuilder.DropTable(
                name: "agent_links");

            migrationBuilder.DropTable(
                name: "agent_templates");

            migrationBuilder.DropIndex(
                name: "ix_agents_template_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "template_id",
                table: "agents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "template_id",
                table: "agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    config = table.Column<string>(type: "text", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    link_type = table.Column<string>(type: "text", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_links_agents_source_agent_id",
                        column: x => x.source_agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_agent_links_agents_target_agent_id",
                        column: x => x.target_agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    default_skills_json = table.Column<string>(type: "text", nullable: false),
                    default_soul_json = table.Column<string>(type: "text", nullable: false),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agents_template_id",
                table: "agents",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_links_source_agent_id_target_agent_id_link_type",
                table: "agent_links",
                columns: new[] { "source_agent_id", "target_agent_id", "link_type" },
                unique: true,
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_links_target_agent_id",
                table: "agent_links",
                column: "target_agent_id");

            migrationBuilder.AddForeignKey(
                name: "fk_agents_agent_templates_template_id",
                table: "agents",
                column: "template_id",
                principalTable: "agent_templates",
                principalColumn: "id");
        }
    }
}
