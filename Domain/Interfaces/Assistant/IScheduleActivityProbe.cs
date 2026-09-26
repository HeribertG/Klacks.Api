// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers whether a group's schedule actually carries anything in a given date range, and how far
/// the installation as a whole has been set up. Period triggers use it to stay silent about a period
/// nobody ever planned: a period with no assignment in it has nothing to close, exactly the way
/// GroupStaffingLookup already keeps them silent about a group nobody works in. Ranges cover the
/// group itself plus every descendant group, because a shift may hang on a child while the payment
/// interval is configured on the parent.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IScheduleActivityProbe
{
    /// <summary>
    /// True when at least one real (non-scenario, not soft-deleted) work assignment falls into
    /// [from, to] for the group or one of its descendants.
    /// </summary>
    Task<bool> HasWorkInRangeAsync(Group group, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one real work assignment falls into [from, to] on a shift that hangs DIRECTLY on
    /// the group (its own GroupItem), descendants excluded. This is the scope a group-aware seal, the day
    /// locks and the payroll export act on (WorkRepository.SealByPeriodAndGroup joins the group's own
    /// GroupItem only), so it answers "would closing this group seal any work at all" - unlike
    /// HasWorkInRangeAsync, which also counts work of child groups.
    /// </summary>
    Task<bool> HasDirectWorkInRangeAsync(Group group, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one staffable shift of the group or one of its descendants is valid within
    /// [from, to]. Answers "is there anything to plan", which is a different question from
    /// "has anything been planned" — a period with shifts but no assignments is precisely what the
    /// scheduling reminder exists for.
    /// </summary>
    Task<bool> HasPlannableShiftsInRangeAsync(Group group, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one real work assignment falls into [from, to] anywhere in the installation,
    /// regardless of group. For detectors that reason about a period rather than about a group: a
    /// period in which nobody was scheduled at all produces a deficit for every single employee, which
    /// is arithmetic rather than a finding.
    /// </summary>
    Task<bool> HasAnyWorkInRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installation-wide setup snapshot along the order -> shift -> assignment chain.
    /// </summary>
    Task<ScheduleSetupState> GetSetupStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// How many employees are on the books on the reference date: type Employee, not soft-deleted, and
    /// holding a membership valid on that day. A COUNT rather than an existence check because the
    /// grouping recommendation is a judgement about workforce size, and the membership window is part
    /// of the definition rather than a refinement of it — a number that included everybody who ever
    /// worked here would put a wrong figure in front of the user and would fire the recommendation in
    /// installations it does not apply to.
    /// </summary>
    /// <param name="referenceDate">The company's own day the membership window is evaluated against.</param>
    Task<int> CountActiveEmployeesAsync(DateOnly referenceDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many staffable, still valid duties carry no group membership at all. Containers are out of
    /// scope - only a task is planned against a group - and so are orders, whose group is assigned when
    /// they are sealed into a shift. A COUNT rather than an existence check because the recommendation
    /// built on it is a judgement about how many duties are concerned, and a single one is normal rather
    /// than a finding.
    /// </summary>
    /// <param name="referenceDate">The company's own day an expired duty is measured against.</param>
    Task<int> CountUngroupedPlannableShiftsAsync(DateOnly referenceDate, CancellationToken cancellationToken = default);
}
