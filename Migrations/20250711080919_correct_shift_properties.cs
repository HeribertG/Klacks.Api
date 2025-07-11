using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class correct_shift_properties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        ALTER TABLE shift 
        ALTER COLUMN travel_time_before 
        TYPE time without time zone 
        USING (travel_time_before || ' minutes')::interval::time;
    ");

            migrationBuilder.Sql(@"
        ALTER TABLE shift 
        ALTER COLUMN travel_time_after 
        TYPE time without time zone 
        USING (travel_time_after || ' minutes')::interval::time;
    ");

            migrationBuilder.AlterColumn<Guid>(
                name: "macro_id",
                table: "shift",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "travel_time_before",
                table: "shift",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "travel_time_after",
                table: "shift",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "macro_id",
                table: "shift",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
