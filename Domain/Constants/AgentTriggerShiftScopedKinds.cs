// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The AgentTriggerKinds whose events expose a Shift id as IAgentTriggerEvent.EntityId. Only for these
/// can the approval chain derive a "last planner" (stage 1) from the Work rows under that shift; every
/// other kind carries no EntityId at all. availability_gap and client_missing_core_data used to carry a
/// Client id and were excluded anyway, because that audit stamp names the master-data maintainer rather
/// than a planner; since both aggregate their findings into one event per period respectively per
/// missing field, they name no single entity and fall under the general case.
/// Curated beside AgentTriggerKinds for the same reason AgentTriggerGroupScopedKinds is:
/// Domain must not depend on the Application layer the event types live in.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentTriggerShiftScopedKinds
{
    public static readonly string[] Values =
    [
        AgentTriggerKinds.UnstaffedShift,
        AgentTriggerKinds.OpenOrder,
        AgentTriggerKinds.UncutFulldayShift,
        AgentTriggerKinds.EmptyContainer
    ];

    public static bool Contains(string triggerKind) =>
        Values.Contains(triggerKind, StringComparer.Ordinal);
}
