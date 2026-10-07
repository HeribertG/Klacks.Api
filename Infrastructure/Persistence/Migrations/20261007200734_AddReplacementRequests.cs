using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReplacementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "replacement_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absent_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    reported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outcome_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
                    work_change_id = table.Column<Guid>(type: "uuid", nullable: true),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_replacement_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_replacement_requests_absent_client_id_date",
                table: "replacement_requests",
                columns: new[] { "absent_client_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_replacement_requests_analyse_token_candidate_client_id_shif",
                table: "replacement_requests",
                columns: new[] { "analyse_token", "candidate_client_id", "shift_id", "date" },
                unique: true,
                filter: "\"is_deleted\" = false")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_replacement_requests_candidate_client_id",
                table: "replacement_requests",
                column: "candidate_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_replacement_requests_reported_at_utc",
                table: "replacement_requests",
                column: "reported_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "replacement_requests");
        }
    }
}
