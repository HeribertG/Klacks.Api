using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErpImportReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_order_reference",
                table: "shift",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_system_id",
                table: "shift",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supersedes_order_id",
                table: "shift",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_customer_reference",
                table: "client",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_system_id",
                table: "client",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_source_system_id_external_order_reference",
                table: "shift",
                columns: new[] { "source_system_id", "external_order_reference" },
                filter: "external_order_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_client_source_system_id_external_customer_reference",
                table: "client",
                columns: new[] { "source_system_id", "external_customer_reference" },
                unique: true,
                filter: "external_customer_reference IS NOT NULL AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shift_source_system_id_external_order_reference",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_client_source_system_id_external_customer_reference",
                table: "client");

            migrationBuilder.DropColumn(
                name: "external_order_reference",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "source_system_id",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "supersedes_order_id",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "external_customer_reference",
                table: "client");

            migrationBuilder.DropColumn(
                name: "source_system_id",
                table: "client");
        }
    }
}
