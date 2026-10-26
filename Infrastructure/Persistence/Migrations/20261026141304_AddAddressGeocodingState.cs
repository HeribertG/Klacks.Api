using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressGeocodingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "address_geocoding_state",
                columns: table => new
                {
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempted_zip = table.Column<string>(type: "text", nullable: true),
                    attempted_city = table.Column<string>(type: "text", nullable: true),
                    attempted_country = table.Column<string>(type: "text", nullable: true),
                    outcome = table.Column<int>(type: "integer", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    requested_by = table.Column<int>(type: "integer", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    filled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fill_source = table.Column<int>(type: "integer", nullable: true),
                    filled_by = table.Column<int>(type: "integer", nullable: true),
                    filled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    filled_latitude = table.Column<double>(type: "double precision", nullable: true),
                    filled_longitude = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_address_geocoding_state", x => x.address_id);
                    table.ForeignKey(
                        name: "fk_address_geocoding_state_address_address_id",
                        column: x => x.address_id,
                        principalTable: "address",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "geocoded_locality",
                columns: table => new
                {
                    country_code = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    found = table.Column<bool>(type: "boolean", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    county = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_geocoded_locality", x => new { x.country_code, x.postal_code, x.city });
                });

            migrationBuilder.CreateIndex(
                name: "ix_address_geocoding_state_requested_at",
                table: "address_geocoding_state",
                column: "requested_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address_geocoding_state");

            migrationBuilder.DropTable(
                name: "geocoded_locality");
        }
    }
}
