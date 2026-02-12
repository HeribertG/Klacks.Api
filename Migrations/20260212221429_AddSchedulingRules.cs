using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS llm_function_definitions (
                    id uuid NOT NULL,
                    name text NOT NULL,
                    description text NOT NULL,
                    parameters_json text NOT NULL,
                    required_permission text,
                    execution_type text NOT NULL,
                    category text NOT NULL,
                    is_enabled boolean NOT NULL,
                    sort_order integer NOT NULL,
                    create_time timestamp with time zone,
                    current_user_created text,
                    current_user_deleted text,
                    current_user_updated text,
                    deleted_time timestamp with time zone,
                    is_deleted boolean NOT NULL,
                    update_time timestamp with time zone,
                    CONSTRAINT pk_llm_function_definitions PRIMARY KEY (id)
                );
            ");

            migrationBuilder.CreateTable(
                name: "scheduling_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_work_days = table.Column<int>(type: "integer", nullable: true),
                    min_rest_days = table.Column<int>(type: "integer", nullable: true),
                    min_pause_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_optimal_gap = table.Column<decimal>(type: "numeric", nullable: true),
                    max_daily_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_weekly_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_consecutive_days = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_scheduling_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduling_rules_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_llm_function_definitions_is_deleted_is_enabled_sort_order
                ON llm_function_definitions (is_deleted, is_enabled, sort_order);
            ");

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rules_contract_id_is_deleted",
                table: "scheduling_rules",
                columns: new[] { "contract_id", "is_deleted" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduling_rules");
        }
    }
}
