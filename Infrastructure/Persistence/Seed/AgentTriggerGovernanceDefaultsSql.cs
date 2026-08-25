// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs one default governance row per governed trigger kind, so an administrator opening the
/// settings card sees a complete table instead of an empty one and no kind starts out unconfigured.
/// The values are the fail-safe defaults - report and wait - which is exactly how the pipeline behaved
/// before governance existed, so applying this changes no behaviour.
/// </summary>
/// <remarks>
/// The rows are inserted with WHERE NOT EXISTS rather than ON CONFLICT: the uniqueness of an
/// installation-wide rule rests on a PARTIAL index, and inferring a conflict target against a partial
/// index would mean restating its predicate for no gain. Re-running is therefore harmless and an
/// administrator's own edits are never overwritten. The kind list is read from
/// ProactiveGovernanceDefaults so the seed cannot drift away from the code.
/// </remarks>

using Klacks.Api.Domain.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class AgentTriggerGovernanceDefaultsSql
    {
        private const string TableName = "agent_trigger_governance";
        private const string SeedUser = "admin";

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            foreach (var triggerKind in ProactiveGovernanceDefaults.GovernedKinds)
            {
                migrationBuilder.Sql(InsertDefaultRow(triggerKind));
            }
        }

        public static void Remove(MigrationBuilder migrationBuilder)
        {
            foreach (var triggerKind in ProactiveGovernanceDefaults.GovernedKinds)
            {
                migrationBuilder.Sql(
                    $"DELETE FROM {TableName} " +
                    $"WHERE trigger_kind = '{triggerKind}' AND group_id IS NULL;");
            }
        }

        private static string InsertDefaultRow(string triggerKind)
        {
            return $@"
INSERT INTO {TableName} (
    id, trigger_kind, group_id, max_action, enabled, responsible_owner_user_id,
    daily_action_budget, window_action_limit, window_minutes,
    create_time, current_user_created, is_deleted)
SELECT
    gen_random_uuid(), '{triggerKind}', NULL, {(int)ProactiveGovernanceDefaults.MaxAction},
    {(ProactiveGovernanceDefaults.Enabled ? "true" : "false")}, NULL,
    {ProactiveGovernanceDefaults.DailyActionBudget}, {ProactiveGovernanceDefaults.WindowActionLimit},
    {ProactiveGovernanceDefaults.WindowMinutes},
    NOW(), '{SeedUser}', false
WHERE NOT EXISTS (
    SELECT 1 FROM {TableName}
    WHERE trigger_kind = '{triggerKind}' AND group_id IS NULL AND is_deleted = false);";
        }
    }
}
