using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentConditionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_condition_groups",
                columns: table => new
                {
                    condition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_condition_groups", x => new { x.condition_id, x.group_id });
                    table.ForeignKey(
                        name: "fk_agent_condition_groups_agent_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "agent_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_condition_groups_group_id",
                table: "agent_condition_groups",
                column: "group_id");

            migrationBuilder.Sql(
                "INSERT INTO agent_condition_groups (condition_id, group_id) " +
                "SELECT id, group_id FROM agent_conditions WHERE group_id IS NOT NULL " +
                "ON CONFLICT DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_condition_groups");
        }
    }
}
