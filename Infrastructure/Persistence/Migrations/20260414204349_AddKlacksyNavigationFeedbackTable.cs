using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKlacksyNavigationFeedbackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "klacksy_navigation_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    utterance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    matched_target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    matched_score = table.Column<double>(type: "double precision", nullable: true),
                    user_action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actual_route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_klacksy_navigation_feedback", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_klacksy_navigation_feedback_matched_target_id",
                table: "klacksy_navigation_feedback",
                column: "matched_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_klacksy_navigation_feedback_timestamp",
                table: "klacksy_navigation_feedback",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "klacksy_navigation_feedback");
        }
    }
}
