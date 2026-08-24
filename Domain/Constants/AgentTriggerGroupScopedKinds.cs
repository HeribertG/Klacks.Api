// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The AgentTriggerKinds whose events declare IAgentTriggerEvent.RequiresGroupScope: findings that are
/// only ever about a group-owned entity, so a ledger row of such a kind carrying no GroupId means "the
/// group could not be determined", never "this concerns everybody". AgentConditionRepository's
/// planner-facing reads use this to withhold those rows from scoped planners and leave them to Admins,
/// the same fallback AgentTriggerService.ResolvePlannerAudienceAsync already applies on the live-push path.
///
/// Curated rather than reflected: the set has to reach EF as a plain string array so Npgsql translates the
/// membership test into SQL, and it belongs beside AgentTriggerKinds in Domain, which must not depend on the
/// Application layer the event types live in. Reading RequiresGroupScope off those types at runtime would
/// also need GetUninitializedObject to sidestep their positional constructors. Drift is caught instead by
/// AgentTriggerGroupScopedKindsGuardTests, which reflects over every IAgentTriggerEvent implementation and
/// asserts set equality in both directions.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentTriggerGroupScopedKinds
{
    public static readonly string[] Values =
    [
        AgentTriggerKinds.OpenOrder,
        AgentTriggerKinds.EmptyContainer,
        AgentTriggerKinds.UncutFulldayShift,
        AgentTriggerKinds.UnstaffedShift,
        AgentTriggerKinds.LockConflict,
        AgentTriggerKinds.WorkDroppedByErpImport
    ];
}
