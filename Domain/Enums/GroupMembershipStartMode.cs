// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How the start date of new group memberships is chosen. Fixed uses one date for every person; EntryDate starts
/// each person's membership on the day of their own employment entry (Membership.ValidFrom), so the schedule shows
/// no day between entry and group start as blue and locked.
/// </summary>
public enum GroupMembershipStartMode
{
    Fixed = 0,
    EntryDate = 1
}
