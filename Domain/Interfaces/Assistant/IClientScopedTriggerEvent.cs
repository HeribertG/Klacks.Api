// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A planner event whose content names employees (clients). Its audience is not a fixed planner list but
/// follows the client group-visibility rule per named employee: AgentTriggerService asks
/// IPlanningAudienceResolver.GetPlanningUserIdsForClientAsync for every affected client and hands each
/// recipient only the variant of the event that is narrowed to the clients that recipient may see. A
/// recipient who may see none of them receives nothing. Admins see every client, so they receive the
/// unnarrowed event.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IClientScopedTriggerEvent : IAgentTriggerEvent
{
    /// <summary>
    /// Every client the event's content names. The audience is the union of the per-client audiences.
    /// </summary>
    IReadOnlyCollection<Guid> AffectedClientIds { get; }

    /// <summary>
    /// The same event restricted to the given visible clients: every count, name list and severity derived
    /// from the affected clients must be recomputed from the narrowed set. Returns null when none of the
    /// affected clients is visible, which means "nothing to tell this recipient". The DedupKey, Summary key
    /// and Kind must stay unchanged, so every variant reports the same condition-ledger row and the
    /// per-recipient dedup keeps working.
    /// </summary>
    /// <param name="visibleClientIds">The affected clients the recipient may see.</param>
    IAgentTriggerEvent? NarrowTo(IReadOnlySet<Guid> visibleClientIds);
}
