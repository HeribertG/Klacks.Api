// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The AgentTriggerKinds whose events expose a Shift id as IAgentTriggerEvent.EntityId. Only for these
/// can the approval chain derive a "last planner" (stage 1) from the Work rows under that shift; the
/// client-scoped kinds (availability_gap, client_missing_core_data) carry a Client id whose audit
/// stamp names the master-data maintainer rather than a planner, and every other kind carries no
/// EntityId at all. Curated beside AgentTriggerKinds for the same reason AgentTriggerGroupScopedKinds is:
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
