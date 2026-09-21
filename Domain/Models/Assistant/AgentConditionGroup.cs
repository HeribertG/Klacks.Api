// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// The complete set of groups one condition-ledger row concerns, one row per group. It exists because
/// <see cref="AgentCondition.GroupId"/> can hold a single group while a shift-borne finding legitimately
/// names several (GroupItem is a many-to-many join), and the planner-facing reads gate visibility on the
/// group: a planner scoped to any other group of a multi-group shift could not find the finding at all,
/// although the live push had correctly reached them. GroupId stays as the row's PRIMARY group - the key
/// the per-group action budget and the governance lookup are counted in, deliberately one stable value -
/// and this table answers the visibility question instead.
///
/// AT DETECTION the two agree: GroupId is the smallest member of this set and null exactly when the set
/// is empty (AgentConditionLedgerPolicy.PrimaryGroupIdFor). They may DIVERGE afterwards, and that is
/// deliberate: a re-observation brings this set in step with what the detector now reports, but never
/// rewrites GroupId, because the per-group budget and the governance decision are counted in it and a
/// bucket that moves between ticks would make both meaningless. So a shift that changed groups keeps
/// charging its old bucket while being visible to its new planners, and a row detected before its group
/// could be determined keeps a null GroupId even once this set fills - the scope reads treat such a row
/// as not group-borne and leave it with Admins, which is the same withholding they applied before this
/// table existed, never anything wider.
///
/// No BaseEntity and no soft delete, matching AgentMemoryTag: these rows are only ever reached through
/// their condition, whose own query filter already hides a soft-deleted ledger row, and the cascading
/// foreign key removes them when the retention purge finally deletes the condition physically. A
/// Restrict foreign key here would instead make that purge's raw DELETE fail for the whole
/// agent_conditions table. GroupId carries an index but NO foreign key to group, exactly as
/// AgentCondition.GroupId does, so purging a soft-deleted group can never fail on this table either.
/// </summary>
public class AgentConditionGroup
{
    public Guid ConditionId { get; set; }

    public Guid GroupId { get; set; }
}
