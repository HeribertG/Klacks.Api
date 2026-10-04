// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The AgentTriggerKinds emitted by IClientAggregateTriggerEvent types: one finding that names several
/// employees. Their condition-ledger row carries the UNNARROWED payload (every affected employee's name and
/// the full count), while each recipient's dispatch row carries only the employees that recipient may see.
/// ProactiveLivePayloadPolicy uses this set to keep the inbox read and the reminder sweep from merging the
/// workforce-wide payload over a supervisor's narrowed parameters.
///
/// Curated rather than reflected for the same reason as AgentTriggerGroupScopedKinds: Domain must not depend
/// on the Application layer the event types live in. AgentTriggerClientAggregateKindsGuardTests reflects over
/// every IAgentTriggerEvent implementation and asserts set equality in both directions.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentTriggerClientAggregateKinds
{
    public static readonly string[] Values =
    [
        AgentTriggerKinds.AvailabilityGap,
        AgentTriggerKinds.ClientMissingCoreData,
        AgentTriggerKinds.TargetHoursDrift
    ];
}
