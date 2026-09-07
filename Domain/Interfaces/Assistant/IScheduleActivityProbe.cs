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
}
