using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWhitelistFromShiftPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM client_shift_preference WHERE preference_type = 0;");
            migrationBuilder.Sql("UPDATE client_shift_preference SET preference_type = 0 WHERE preference_type = 1;");
            migrationBuilder.Sql("UPDATE client_shift_preference SET preference_type = 1 WHERE preference_type = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
