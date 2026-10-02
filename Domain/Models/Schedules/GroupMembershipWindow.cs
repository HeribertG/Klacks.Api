// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The validity window of one employee's membership in a group subtree: the employee's own Membership
/// period intersected with the GroupItem's optional validity. Used to decide who is an active member on a
/// given day, e.g. for detecting company holidays (every active member away the whole day).
/// </summary>
/// <param name="ClientId">The employee the window belongs to</param>
/// <param name="MembershipFrom">First day of the employee's Membership</param>
/// <param name="MembershipUntil">Last day of the employee's Membership, null when open-ended</param>
/// <param name="GroupItemFrom">First day of the group assignment, null when unrestricted</param>
/// <param name="GroupItemUntil">Last day of the group assignment, null when unrestricted</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record GroupMembershipWindow(
    Guid ClientId,
    DateOnly MembershipFrom,
    DateOnly? MembershipUntil,
    DateOnly? GroupItemFrom,
    DateOnly? GroupItemUntil)
{
    /// <summary>True when both the Membership and the group assignment are valid on the given day.</summary>
    /// <param name="day">The calendar day to test</param>
    public bool IsActiveOn(DateOnly day) =>
        MembershipFrom <= day
        && (MembershipUntil == null || MembershipUntil.Value >= day)
        && (GroupItemFrom == null || GroupItemFrom.Value <= day)
        && (GroupItemUntil == null || GroupItemUntil.Value >= day);
}
