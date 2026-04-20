using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyseTokenToClientPeriodHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date",
                table: "client_period_hours");

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "client_period_hours",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date_analyse_t",
                table: "client_period_hours",
                columns: new[] { "client_id", "start_date", "end_date", "analyse_token" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date_analyse_t",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "client_period_hours");

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date",
                table: "client_period_hours",
                columns: new[] { "client_id", "start_date", "end_date" },
                unique: true);
        }
    }
}
