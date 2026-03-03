using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedEmailTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "received_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "text", nullable: false),
                    imap_uid = table.Column<long>(type: "bigint", nullable: false),
                    folder = table.Column<string>(type: "text", nullable: false),
                    from_address = table.Column<string>(type: "text", nullable: false),
                    from_name = table.Column<string>(type: "text", nullable: true),
                    to_address = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    has_attachments = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_received_emails", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_folder_imap_uid",
                table: "received_emails",
                columns: new[] { "folder", "imap_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_is_deleted_is_read",
                table: "received_emails",
                columns: new[] { "is_deleted", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_is_deleted_received_date",
                table: "received_emails",
                columns: new[] { "is_deleted", "received_date" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_message_id",
                table: "received_emails",
                column: "message_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "received_emails");
        }
    }
}
