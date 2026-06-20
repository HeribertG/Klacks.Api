using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToAgentMemory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "agent_memories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id_user_id",
                table: "agent_memories",
                columns: new[] { "agent_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_agent_memories_agent_id_user_id",
                table: "agent_memories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "agent_memories");
        }
    }
}
