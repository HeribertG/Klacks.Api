using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmailAnalysisToInboundAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_email_analyses_received_emails_received_email_id",
                table: "email_analyses");

            migrationBuilder.DropIndex(
                name: "ix_email_analyses_received_email_id",
                table: "email_analyses");

            migrationBuilder.RenameTable(
                name: "email_analyses",
                newName: "inbound_analyses");

            // Postgres RENAME TABLE does not rename the primary key constraint or the surviving
            // indexes, so they are renamed explicitly to keep the physical schema in sync with the
            // model snapshot (ix_inbound_analyses_*, pk_inbound_analyses) - purely cosmetic, but
            // otherwise every future `dotnet ef migrations add` would see a spurious pending rename.
            migrationBuilder.Sql(
                "ALTER TABLE inbound_analyses RENAME CONSTRAINT pk_email_analyses TO pk_inbound_analyses;");

            migrationBuilder.RenameIndex(
                name: "ix_email_analyses_is_deleted_client_id",
                table: "inbound_analyses",
                newName: "ix_inbound_analyses_is_deleted_client_id");

            migrationBuilder.RenameIndex(
                name: "ix_email_analyses_is_deleted_intent",
                table: "inbound_analyses",
                newName: "ix_inbound_analyses_is_deleted_intent");

            migrationBuilder.RenameColumn(
                name: "received_email_id",
                table: "inbound_analyses",
                newName: "source_id");

            migrationBuilder.AddColumn<int>(
                name: "source_kind",
                table: "inbound_analyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "channel",
                table: "inbound_analyses",
                type: "text",
                nullable: false,
                defaultValue: "Email");

            migrationBuilder.CreateIndex(
                name: "ix_inbound_analyses_source_kind_source_id",
                table: "inbound_analyses",
                columns: new[] { "source_kind", "source_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_inbound_analyses_source_kind_source_id",
                table: "inbound_analyses");

            migrationBuilder.DropColumn(
                name: "channel",
                table: "inbound_analyses");

            migrationBuilder.DropColumn(
                name: "source_kind",
                table: "inbound_analyses");

            migrationBuilder.RenameColumn(
                name: "source_id",
                table: "inbound_analyses",
                newName: "received_email_id");

            migrationBuilder.RenameIndex(
                name: "ix_inbound_analyses_is_deleted_intent",
                table: "inbound_analyses",
                newName: "ix_email_analyses_is_deleted_intent");

            migrationBuilder.RenameIndex(
                name: "ix_inbound_analyses_is_deleted_client_id",
                table: "inbound_analyses",
                newName: "ix_email_analyses_is_deleted_client_id");

            migrationBuilder.Sql(
                "ALTER TABLE inbound_analyses RENAME CONSTRAINT pk_inbound_analyses TO pk_email_analyses;");

            migrationBuilder.RenameTable(
                name: "inbound_analyses",
                newName: "email_analyses");

            migrationBuilder.CreateIndex(
                name: "ix_email_analyses_received_email_id",
                table: "email_analyses",
                column: "received_email_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_email_analyses_received_emails_received_email_id",
                table: "email_analyses",
                column: "received_email_id",
                principalTable: "received_emails",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
