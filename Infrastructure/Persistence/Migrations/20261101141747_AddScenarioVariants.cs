using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_variant_shift",
                table: "shift_day_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "analyse_scenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "analyse_scenario_variants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario_token = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_analyse_scenario_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_analyse_scenario_variants_analyse_scenarios_variant_id",
                        column: x => x.variant_id,
                        principalTable: "analyse_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenarios_kind_group_id",
                table: "analyse_scenarios",
                columns: new[] { "kind", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenario_variants_scenario_token_variant_id",
                table: "analyse_scenario_variants",
                columns: new[] { "scenario_token", "variant_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenario_variants_variant_id",
                table: "analyse_scenario_variants",
                column: "variant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analyse_scenario_variants");

            migrationBuilder.DropIndex(
                name: "ix_analyse_scenarios_kind_group_id",
                table: "analyse_scenarios");

            migrationBuilder.DropColumn(
                name: "is_variant_shift",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "analyse_scenarios");
        }
    }
}
