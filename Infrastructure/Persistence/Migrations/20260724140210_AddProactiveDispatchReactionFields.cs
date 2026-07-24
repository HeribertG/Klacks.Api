using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProactiveDispatchReactionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_key",
                table: "agent_trigger_dispatches",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "content_params_json",
                table: "agent_trigger_dispatches",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reaction",
                table: "agent_trigger_dispatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "reaction_at_utc",
                table: "agent_trigger_dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "severity",
                table: "agent_trigger_dispatches",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_key",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "content_params_json",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "reaction",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "reaction_at_utc",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "severity",
                table: "agent_trigger_dispatches");
        }
    }
}
