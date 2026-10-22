// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A planner event that aggregates many group-owned entities (for example every open order) into ONE
/// finding, whose counts differ per recipient because each planner may only see the entities of the groups
/// their GroupVisibility covers. It is the group counterpart of IClientScopedTriggerEvent: AgentTriggerService
/// resolves, per group the event names, the planners who may see that group
/// (IPlanningAudienceResolver.GetPlanningUserIdsForGroupAsync) and hands each recipient only the variant
/// narrowed to the groups that recipient may see. Entities without any group reach Admins only, so only an
/// Admin's variant is narrowed with includesUngrouped set. A recipient who may see none of the entities
/// receives nothing.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGroupScopedAggregateTriggerEvent : IAgentTriggerEvent
{
    /// <summary>
    /// The same event restricted to the entities a recipient may see: every count and the severity must be
    /// recomputed from the narrowed set. Returns null when nothing is left, which means "nothing to tell this
    /// recipient". Kind, Summary key and DedupKey must stay unchanged, so every variant reports the same
    /// condition-ledger row and the per-recipient dedup keeps working.
    /// </summary>
    /// <param name="visibleGroupIds">The groups of this event the recipient may see.</param>
    /// <param name="includesUngrouped">True for a recipient who may also see entities without any group (an Admin).</param>
    IAgentTriggerEvent? NarrowTo(IReadOnlySet<Guid> visibleGroupIds, bool includesUngrouped);
}
