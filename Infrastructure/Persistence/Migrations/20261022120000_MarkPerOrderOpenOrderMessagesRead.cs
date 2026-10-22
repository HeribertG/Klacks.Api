using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Marks every still-unread proactive inbox message of the former per-order open-order shape as read, once, on
    /// deploy (owner decision 2026-10-22). Those messages (one per order, content key
    /// i18n:assistant.proactive.openOrder) were replaced by one daily summary per planner
    /// (i18n:assistant.proactive.openOrdersSummary); their ledger rows are resolved by the first trigger tick, but
    /// the inbox rows would otherwise stay unread and inflate the unread badge until the 180-day retention removes
    /// them. Scoped to exactly that kind and content key, so a summary message is never touched; nothing is
    /// deleted. Idempotent: only rows without a read stamp are updated. The literals are frozen on purpose - a
    /// migration must keep meaning what it meant when it was written, whatever the constants become later.
    /// Down is a no-op: which rows were unread before is not recorded, and re-marking them unread would bring
    /// the flood back.
    /// </summary>
    public partial class MarkPerOrderOpenOrderMessagesRead : Migration
    {
        private const string OpenOrderTriggerKind = "open_order";
        private const string PerOrderContentKey = "i18n:assistant.proactive.openOrder";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE agent_trigger_dispatches
                SET read_at_utc = now()
                WHERE trigger_kind = '{OpenOrderTriggerKind}'
                  AND content_key = '{PerOrderContentKey}'
                  AND read_at_utc IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
