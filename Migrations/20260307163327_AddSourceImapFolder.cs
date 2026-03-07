using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceImapFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "source_imap_folder",
                table: "received_emails",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE received_emails
                SET source_imap_folder = CASE
                    WHEN folder = 'client-assigned' THEN 'INBOX'
                    ELSE folder
                END
                WHERE source_imap_folder = '';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_source_imap_folder_imap_uid",
                table: "received_emails",
                columns: new[] { "source_imap_folder", "imap_uid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_received_emails_source_imap_folder_imap_uid",
                table: "received_emails");

            migrationBuilder.DropColumn(
                name: "source_imap_folder",
                table: "received_emails");
        }
    }
}
