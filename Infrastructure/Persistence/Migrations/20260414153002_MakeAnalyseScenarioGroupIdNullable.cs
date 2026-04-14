using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeAnalyseScenarioGroupIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_analyse_scenarios_group_group_id",
                table: "analyse_scenarios");

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "analyse_scenarios",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_analyse_scenarios_group_group_id",
                table: "analyse_scenarios",
                column: "group_id",
                principalTable: "group",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_analyse_scenarios_group_group_id",
                table: "analyse_scenarios");

            migrationBuilder.AlterColumn<Guid>(
                name: "group_id",
                table: "analyse_scenarios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_analyse_scenarios_group_group_id",
                table: "analyse_scenarios",
                column: "group_id",
                principalTable: "group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
