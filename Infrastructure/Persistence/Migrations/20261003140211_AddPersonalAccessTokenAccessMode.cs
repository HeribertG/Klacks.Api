using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalAccessTokenAccessMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "access_mode",
                table: "personal_access_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Tokens issued before access modes existed keep full tool use (Write = 1), so existing
            // integrations do not lose write access silently. New rows default to Read (0).
            migrationBuilder.Sql("UPDATE personal_access_tokens SET access_mode = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_mode",
                table: "personal_access_tokens");
        }
    }
}
