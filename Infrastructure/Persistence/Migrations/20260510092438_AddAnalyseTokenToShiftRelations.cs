using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyseTokenToShiftRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "shift_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "client_shift_preference",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_expenses_analyse_token",
                table: "shift_expenses",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_analyse_token",
                table: "client_shift_preference",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shift_expenses_analyse_token",
                table: "shift_expenses");

            migrationBuilder.DropIndex(
                name: "ix_client_shift_preference_analyse_token",
                table: "client_shift_preference");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "shift_expenses");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "client_shift_preference");
        }
    }
}
