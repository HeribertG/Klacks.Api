using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedEmailThreadHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "in_reply_to",
                table: "received_emails",
                type: "character varying(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_generated",
                table: "received_emails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "thread_references",
                table: "received_emails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "in_reply_to",
                table: "received_emails");

            migrationBuilder.DropColumn(
                name: "is_auto_generated",
                table: "received_emails");

            migrationBuilder.DropColumn(
                name: "thread_references",
                table: "received_emails");
        }
    }
}
