using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyseTokenToWorkChangeAndExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "work_change",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_change_analyse_token",
                table: "work_change",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_analyse_token",
                table: "expenses",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_change_analyse_token",
                table: "work_change");

            migrationBuilder.DropIndex(
                name: "ix_expenses_analyse_token",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "work_change");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "expenses");
        }
    }
}
