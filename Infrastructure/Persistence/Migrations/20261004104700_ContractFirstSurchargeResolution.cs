using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Contract-first resolution of the five surcharge time-credit rates and the shift-work flag (owner
    /// decision 2026-10-04): contract.performs_shift_work becomes nullable (null = standard from the
    /// scheduling rule, then settings). Before this change a scheduling rule value beat the contract; the
    /// data fix clears a contract value to NULL where a live rule (or, for a rate, any live dated rate
    /// revision of that rule) defines the same field, because the rule won there. This is an approximation:
    /// for dates before such a revision the contract rate used to apply. Deliberate behaviour changes that
    /// the fix does not undo: a contract whose effective shift-work flag is false loses its own rates to
    /// the standard. Stored work surcharges are not recalculated (owner decision: product not yet
    /// published). Idempotent. Down restores NOT NULL after filling NULLs with false; the cleared contract
    /// values are not restored.
    /// </summary>
    public partial class ContractFirstSurchargeResolution : Migration
    {
        private const string ClearShiftWorkDefinedByRuleSql = @"
UPDATE contract c
SET performs_shift_work = NULL
FROM scheduling_rules r
WHERE r.id = c.scheduling_rule_id
  AND r.is_deleted = false
  AND r.performs_shift_work IS NOT NULL
  AND c.performs_shift_work IS NOT NULL;";

        private const string ClearRatesDefinedByRuleSql = @"
UPDATE contract c
SET night_rate = NULL
WHERE c.night_rate IS NOT NULL
  AND c.scheduling_rule_id IS NOT NULL
  AND (EXISTS (SELECT 1 FROM scheduling_rules r
               WHERE r.id = c.scheduling_rule_id AND r.is_deleted = false AND r.night_rate IS NOT NULL)
       OR EXISTS (SELECT 1 FROM scheduling_rule_rate_revisions v
                  WHERE v.scheduling_rule_id = c.scheduling_rule_id AND v.is_deleted = false AND v.night_rate IS NOT NULL));

UPDATE contract c
SET holiday_rate = NULL
WHERE c.holiday_rate IS NOT NULL
  AND c.scheduling_rule_id IS NOT NULL
  AND (EXISTS (SELECT 1 FROM scheduling_rules r
               WHERE r.id = c.scheduling_rule_id AND r.is_deleted = false AND r.holiday_rate IS NOT NULL)
       OR EXISTS (SELECT 1 FROM scheduling_rule_rate_revisions v
                  WHERE v.scheduling_rule_id = c.scheduling_rule_id AND v.is_deleted = false AND v.holiday_rate IS NOT NULL));

UPDATE contract c
SET we1rate = NULL
WHERE c.we1rate IS NOT NULL
  AND c.scheduling_rule_id IS NOT NULL
  AND (EXISTS (SELECT 1 FROM scheduling_rules r
               WHERE r.id = c.scheduling_rule_id AND r.is_deleted = false AND r.we1rate IS NOT NULL)
       OR EXISTS (SELECT 1 FROM scheduling_rule_rate_revisions v
                  WHERE v.scheduling_rule_id = c.scheduling_rule_id AND v.is_deleted = false AND v.we1rate IS NOT NULL));

UPDATE contract c
SET we2rate = NULL
WHERE c.we2rate IS NOT NULL
  AND c.scheduling_rule_id IS NOT NULL
  AND (EXISTS (SELECT 1 FROM scheduling_rules r
               WHERE r.id = c.scheduling_rule_id AND r.is_deleted = false AND r.we2rate IS NOT NULL)
       OR EXISTS (SELECT 1 FROM scheduling_rule_rate_revisions v
                  WHERE v.scheduling_rule_id = c.scheduling_rule_id AND v.is_deleted = false AND v.we2rate IS NOT NULL));

UPDATE contract c
SET we3rate = NULL
WHERE c.we3rate IS NOT NULL
  AND c.scheduling_rule_id IS NOT NULL
  AND (EXISTS (SELECT 1 FROM scheduling_rules r
               WHERE r.id = c.scheduling_rule_id AND r.is_deleted = false AND r.we3rate IS NOT NULL)
       OR EXISTS (SELECT 1 FROM scheduling_rule_rate_revisions v
                  WHERE v.scheduling_rule_id = c.scheduling_rule_id AND v.is_deleted = false AND v.we3rate IS NOT NULL));";

        private const string FillShiftWorkBeforeNotNullSql = @"
UPDATE contract
SET performs_shift_work = false
WHERE performs_shift_work IS NULL;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "performs_shift_work",
                table: "contract",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.Sql(ClearShiftWorkDefinedByRuleSql);
            migrationBuilder.Sql(ClearRatesDefinedByRuleSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(FillShiftWorkBeforeNotNullSql);

            migrationBuilder.AlterColumn<bool>(
                name: "performs_shift_work",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);
        }
    }
}
