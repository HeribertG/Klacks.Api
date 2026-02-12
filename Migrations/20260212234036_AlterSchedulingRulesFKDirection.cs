using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlterSchedulingRulesFKDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_scheduling_rules_contract_contract_id",
                table: "scheduling_rules");

            migrationBuilder.DropIndex(
                name: "ix_scheduling_rules_contract_id_is_deleted",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "contract_id",
                table: "scheduling_rules");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "scheduling_rules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "scheduling_rule_id",
                table: "contract",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rules_is_deleted_name",
                table: "scheduling_rules",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_scheduling_rule_id",
                table: "contract",
                column: "scheduling_rule_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_scheduling_rules_scheduling_rule_id",
                table: "contract",
                column: "scheduling_rule_id",
                principalTable: "scheduling_rules",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_scheduling_rules_scheduling_rule_id",
                table: "contract");

            migrationBuilder.DropIndex(
                name: "ix_scheduling_rules_is_deleted_name",
                table: "scheduling_rules");

            migrationBuilder.DropIndex(
                name: "ix_contract_scheduling_rule_id",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "name",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "scheduling_rule_id",
                table: "contract");

            migrationBuilder.AddColumn<Guid>(
                name: "contract_id",
                table: "scheduling_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rules_contract_id_is_deleted",
                table: "scheduling_rules",
                columns: new[] { "contract_id", "is_deleted" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_scheduling_rules_contract_contract_id",
                table: "scheduling_rules",
                column: "contract_id",
                principalTable: "contract",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
