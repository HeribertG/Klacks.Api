using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimeRangeShiftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sporadic_start_shift",
                table: "shift",
                newName: "time_range_start_shift");

            migrationBuilder.RenameColumn(
                name: "sporadic_end_shift",
                table: "shift",
                newName: "time_range_end_shift");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "time_range_start_shift",
                table: "shift",
                newName: "sporadic_start_shift");

            migrationBuilder.RenameColumn(
                name: "time_range_end_shift",
                table: "shift",
                newName: "sporadic_end_shift");
        }
    }
}
