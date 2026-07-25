using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedEmailProcessedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "processed_at",
                table: "received_emails",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: mark every existing row as already processed, except ones with no analysis row
            // that also never reached the junk folder (junk is intentionally never analyzed). Folder
            // alone can't distinguish "assigned but analysis never ran" from "still fully untouched" —
            // a prior interrupted cycle can leave a row already moved to client-assigned with no
            // analysis at all — so the analysis row's existence is the only reliable completeness marker.
            migrationBuilder.Sql(@"
                UPDATE received_emails r
                SET processed_at = now()
                WHERE r.processed_at IS NULL
                  AND (
                    r.folder = (SELECT imap_folder_name FROM email_folders WHERE special_use = 'Junk' AND is_deleted = false LIMIT 1)
                    OR EXISTS (SELECT 1 FROM email_analyses ea WHERE ea.received_email_id = r.id AND ea.is_deleted = false)
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "received_emails");
        }
    }
}
