using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserDirectoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "directory_external_id",
                table: "AspNetUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "identity_provider_id",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_identity_provider_id_directory_external_id",
                table: "AspNetUsers",
                columns: new[] { "identity_provider_id", "directory_external_id" },
                unique: true,
                filter: "\"identity_provider_id\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_identity_providers_identity_provider_id",
                table: "AspNetUsers",
                column: "identity_provider_id",
                principalTable: "identity_providers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_identity_providers_identity_provider_id",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_identity_provider_id_directory_external_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "directory_external_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "identity_provider_id",
                table: "AspNetUsers");
        }
    }
}
