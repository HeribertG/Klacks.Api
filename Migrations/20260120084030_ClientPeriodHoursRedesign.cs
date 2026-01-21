using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class ClientPeriodHoursRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_individual_period_id",
                table: "client_period_hours");

            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_year_month",
                table: "client_period_hours");

            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_year_week_number",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "month",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "week_number",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "year",
                table: "client_period_hours");

            migrationBuilder.AddColumn<DateTime>(
                name: "calculated_at",
                table: "client_period_hours",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateOnly>(
                name: "end_date",
                table: "client_period_hours",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "start_date",
                table: "client_period_hours",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date",
                table: "client_period_hours",
                columns: new[] { "client_id", "start_date", "end_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "calculated_at",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "client_period_hours");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "client_period_hours");

            migrationBuilder.AddColumn<int>(
                name: "month",
                table: "client_period_hours",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "week_number",
                table: "client_period_hours",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "year",
                table: "client_period_hours",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_individual_period_id",
                table: "client_period_hours",
                columns: new[] { "client_id", "individual_period_id" },
                unique: true,
                filter: "payment_interval = 3");

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_year_month",
                table: "client_period_hours",
                columns: new[] { "client_id", "year", "month" },
                unique: true,
                filter: "payment_interval = 2");

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_year_week_number",
                table: "client_period_hours",
                columns: new[] { "client_id", "year", "week_number" },
                unique: true,
                filter: "payment_interval IN (0, 1)");
        }
    }
}
