using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the daily work frame (first start to last end of a work day, pauses included) to the scheduling rule and
    /// backfills the global value 14 for installations that unambiguously run the Swiss region package (CH ArG
    /// Art. 10 Abs. 3): the package country is ch, or the locale country is CH and the locale and worktime sections
    /// of a region package were applied. Every other installation keeps no value, i.e. 24h minus the minimum daily
    /// rest. WHERE NOT EXISTS because ix_settings_type is unique and a value set by hand must win.
    /// </summary>
    public partial class AddSchedulingRuleMaxDailySpanHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "max_daily_span_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.Sql(
                "INSERT INTO settings (id,type,value) " +
                "SELECT gen_random_uuid(),'SCHEDULING_MAX_DAILY_SPAN_HOURS','14' " +
                "WHERE NOT EXISTS (SELECT 1 FROM settings WHERE type='SCHEDULING_MAX_DAILY_SPAN_HOURS') " +
                "AND (" +
                "EXISTS (SELECT 1 FROM settings WHERE type='REGION_PACKAGE_COUNTRY' AND lower(trim(value))='ch') " +
                "OR (EXISTS (SELECT 1 FROM settings WHERE type='APP_ADDRESS_COUNTRY' AND upper(trim(value))='CH') " +
                "AND EXISTS (SELECT 1 FROM settings WHERE type='REGION_SETUP_APPLIED_LOCALE') " +
                "AND EXISTS (SELECT 1 FROM settings WHERE type='REGION_SETUP_APPLIED_WORKTIME')))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM settings WHERE type='SCHEDULING_MAX_DAILY_SPAN_HOURS'");

            migrationBuilder.DropColumn(
                name: "max_daily_span_hours",
                table: "scheduling_rules");
        }
    }
}
