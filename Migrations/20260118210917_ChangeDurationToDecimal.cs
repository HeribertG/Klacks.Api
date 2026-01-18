using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDurationToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE absence_detail ALTER COLUMN duration DROP DEFAULT");
            migrationBuilder.Sql(
                "ALTER TABLE absence_detail ALTER COLUMN duration TYPE numeric USING EXTRACT(EPOCH FROM duration) / 3600");
            migrationBuilder.Sql("ALTER TABLE absence_detail ALTER COLUMN duration SET DEFAULT 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "duration",
                table: "absence_detail",
                type: "interval",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
