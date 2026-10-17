using System;
using Klacks.Api.Domain.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Creates user_intro_tour_states (one intro-tour state per account, cascading with AspNetUsers) and gives every
    /// account that already exists a legacy_offer row: existing accounts get exactly one intro offer instead of the
    /// automatic intro of a new account (an account without a row). Idempotent: ON CONFLICT DO NOTHING keeps any row
    /// that is already there. Down drops the table and with it the backfilled rows.
    /// </summary>
    public partial class AddUserIntroTourStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_intro_tour_states",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    completed_stations = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    snooze_count = table.Column<int>(type: "integer", nullable: false),
                    last_offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_intro_tour_states", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_intro_tour_states_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql($@"
                INSERT INTO user_intro_tour_states (user_id, status, completed_stations, snooze_count, create_time)
                SELECT u.id, '{IntroTourStatus.LegacyOffer}', '[]'::jsonb, 0, now()
                FROM ""AspNetUsers"" u
                ON CONFLICT (user_id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_intro_tour_states");
        }
    }
}
