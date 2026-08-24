// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Re-derives the frontend action route for a ledger-tracked TriggerKind, because AgentCondition does
/// not persist IAgentTriggerEvent.ActionRoute itself - AgentTriggerBackgroundService serializes only
/// triggerEvent.Payload into AgentCondition.PayloadJson (see RunDetectorAsync), never ActionRoute or
/// ActionParams. Every route below was read directly off the concrete TriggerEvent record's ActionRoute
/// property, and every one of them is a per-Kind constant, never computed from instance data, so this
/// mapping is exact today - not a guess. It is however a SEPARATE COPY of that fact: a future
/// TriggerEvent that makes its ActionRoute depend on instance data, or a new detector kind whose entry
/// here is forgotten, will drift silently. AgentConditionActionRoutesTests pins every ledger-tracked
/// kind to a non-null entry so a missing one fails a test instead of shipping quietly. ActionParams
/// (e.g. which groupId/date to preselect) is not recoverable at all from the ledger row, so
/// list_open_findings can only offer a bare route, not the one-click-with-context navigation the live
/// proactive notification gets.
/// </summary>

using System.Collections.Generic;

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionActionRoutes
{
    private static readonly IReadOnlyDictionary<string, string> RouteByTriggerKind = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [AgentTriggerKinds.UnstaffedShift] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.LockConflict] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.TargetHoursDrift] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.ScenarioPending] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.PeriodCloseDue] = ProactiveActionRoutes.PeriodClosing,
        [AgentTriggerKinds.ContractExpiringSoon] = ProactiveActionRoutes.ClientEdit,
        [AgentTriggerKinds.OpenOrder] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.UncutFulldayShift] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.EmptyContainer] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.AvailabilityGap] = ProactiveActionRoutes.ClientAvailability,
        [AgentTriggerKinds.PeriodOverdue] = ProactiveActionRoutes.PeriodClosing,
        [AgentTriggerKinds.ClientMissingCoreData] = ProactiveActionRoutes.ClientEdit,
    };

    public static string? For(string triggerKind) =>
        RouteByTriggerKind.TryGetValue(triggerKind, out var route) ? route : null;
}
