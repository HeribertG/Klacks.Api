// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Re-derives the frontend action route for a ledger-tracked TriggerKind, because AgentCondition does
/// not persist IAgentTriggerEvent.ActionRoute itself - AgentTriggerBackgroundService serializes only
/// triggerEvent.Payload into AgentCondition.PayloadJson (see RunDetectorAsync), never ActionRoute or
/// ActionParams. Almost every route below is a per-Kind constant read directly off the concrete
/// TriggerEvent record's ActionRoute property. The one exception is empty_container, whose route
/// carries the container shift id as path segment (the slot-template page has no route without it);
/// that id is the ledger row's EntityId, so it is rebuilt through the same
/// ProactiveActionRoutes.ContainerTemplateFor the event itself uses. This mapping is still a SEPARATE
/// COPY of each event's route: a new detector kind whose entry is forgotten, or an event whose route
/// starts depending on other instance data, will drift silently. AgentConditionActionRoutesTests pins
/// every ledger-tracked kind to a non-null entry and the empty_container route to the event's own, so
/// a missing or diverging one fails a test instead of shipping quietly. ActionParams (e.g. which
/// groupId/date to preselect) is not recoverable at all from the ledger row, so list_open_findings can
/// only offer a bare route, not the one-click-with-context navigation the live proactive notification gets.
/// </summary>
/// <param name="triggerKind">The ledger row's TriggerKind.</param>
/// <param name="entityId">The ledger row's EntityId; only entity-bound routes read it.</param>

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
        [AgentTriggerKinds.EmptyContainer] = ProactiveActionRoutes.ShiftList,
        [AgentTriggerKinds.AvailabilityGap] = ProactiveActionRoutes.ClientAvailability,
        [AgentTriggerKinds.PeriodOverdue] = ProactiveActionRoutes.PeriodClosing,
        [AgentTriggerKinds.PeriodAutoClose] = ProactiveActionRoutes.PeriodClosing,
        [AgentTriggerKinds.ClientMissingCoreData] = ProactiveActionRoutes.ClientList,
        [AgentTriggerKinds.NoScheduleYet] = ProactiveActionRoutes.Schedule,
        [AgentTriggerKinds.UngroupedWorkforce] = ProactiveActionRoutes.GroupList,
        [AgentTriggerKinds.UngroupedShifts] = ProactiveActionRoutes.ShiftList,
        [AgentTriggerKinds.GroupingFeasibility] = ProactiveActionRoutes.GroupList,
    };

    public static string? For(string triggerKind, Guid? entityId)
    {
        if (string.Equals(triggerKind, AgentTriggerKinds.EmptyContainer, StringComparison.Ordinal) && entityId.HasValue)
        {
            return ProactiveActionRoutes.ContainerTemplateFor(entityId.Value);
        }

        return RouteByTriggerKind.TryGetValue(triggerKind, out var route) ? route : null;
    }
}
