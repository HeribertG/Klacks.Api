using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthAuthorizationCodeStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oauth_authorization_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    client_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    client_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    redirect_uri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    code_challenge = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    scope = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oauth_authorization_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oauth_authorization_codes_code",
                table: "oauth_authorization_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oauth_authorization_codes_expires_at_utc",
                table: "oauth_authorization_codes",
                column: "expires_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oauth_authorization_codes");
        }
    }
}
