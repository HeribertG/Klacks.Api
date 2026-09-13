using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Raises the installation-wide next_period_scheduling_due row from Hint (0) to Execute (2). Before
    /// this release the next-period autofill path never read its governance row at all, so every
    /// installation carries the row seeded by AgentTriggerGovernanceDefaultsSql at the old universal
    /// Hint default; now that INextPeriodAutonomyResolver consults it, that stale Hint would silently
    /// cap the autofill below what the global autonomy level and the admins' own preferences already
    /// allow. Only the installation-wide row (group_id IS NULL) is touched: a per-group exception with
    /// max_action = 0 is a deliberate admin choice made after this row started being read, not a seed
    /// artifact, and must not be overwritten. New installations get Execute directly from the updated
    /// seed (AgentTriggerGovernanceDefaultsSql / ProactiveGovernanceDefaults.SeededMaxActionOverrides).
    /// </summary>
    public partial class RaiseNextPeriodGovernanceSeedToExecute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE agent_trigger_governance
                SET max_action = 2
                WHERE trigger_kind = 'next_period_scheduling_due'
                  AND group_id IS NULL
                  AND max_action = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE agent_trigger_governance
                SET max_action = 0
                WHERE trigger_kind = 'next_period_scheduling_due'
                  AND group_id IS NULL
                  AND max_action = 2;");
        }
    }
}
