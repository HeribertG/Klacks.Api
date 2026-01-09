using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLdapFieldsToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "identity_provider_id",
                table: "client",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ldap_external_id",
                table: "client",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_identity_provider_id",
                table: "client",
                column: "identity_provider_id");

            migrationBuilder.AddForeignKey(
                name: "fk_client_identity_providers_identity_provider_id",
                table: "client",
                column: "identity_provider_id",
                principalTable: "identity_providers",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_client_identity_providers_identity_provider_id",
                table: "client");

            migrationBuilder.DropIndex(
                name: "ix_client_identity_provider_id",
                table: "client");

            migrationBuilder.DropColumn(
                name: "identity_provider_id",
                table: "client");

            migrationBuilder.DropColumn(
                name: "ldap_external_id",
                table: "client");
        }
    }
}
