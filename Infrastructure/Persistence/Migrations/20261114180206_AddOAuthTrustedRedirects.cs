using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// OAuth hardening: authorization codes carry the access mode bound at consent and lose the client name and scope
    /// (name and scope now come from the trusted-list entry and the mode); oauth_clients loses client_name, which is no longer stored; personal access tokens record whether the OAuth
    /// server issued them, the tool name and the redirect host. Existing tokens named "oauth:..." are marked as OAuth-issued
    /// so that switching OAuth connections off also stops them; their tool name and host stay empty.
    /// </summary>
    public partial class AddOAuthTrustedRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "client_name",
                table: "oauth_clients");

            migrationBuilder.DropColumn(
                name: "client_name",
                table: "oauth_authorization_codes");

            migrationBuilder.DropColumn(
                name: "scope",
                table: "oauth_authorization_codes");

            migrationBuilder.AddColumn<bool>(
                name: "is_o_auth_issued",
                table: "personal_access_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "o_auth_redirect_host",
                table: "personal_access_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "o_auth_tool_name",
                table: "personal_access_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "access_mode",
                table: "oauth_authorization_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE personal_access_tokens SET is_o_auth_issued = TRUE WHERE name LIKE 'oauth:%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "client_name",
                table: "oauth_clients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "is_o_auth_issued",
                table: "personal_access_tokens");

            migrationBuilder.DropColumn(
                name: "o_auth_redirect_host",
                table: "personal_access_tokens");

            migrationBuilder.DropColumn(
                name: "o_auth_tool_name",
                table: "personal_access_tokens");

            migrationBuilder.DropColumn(
                name: "access_mode",
                table: "oauth_authorization_codes");

            migrationBuilder.AddColumn<string>(
                name: "client_name",
                table: "oauth_authorization_codes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "scope",
                table: "oauth_authorization_codes",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
