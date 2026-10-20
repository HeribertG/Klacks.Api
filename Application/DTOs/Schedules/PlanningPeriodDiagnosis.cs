// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Why an employee can or cannot be planned on a range of days from the planning periods alone: the employment
/// (membership) period, the group membership periods and the day seals. Entries, qualifications and working-time rules
/// are not part of it.
/// </summary>
/// <param name="Person">Display name of the employee</param>
/// <param name="Employment">The employment window, null when the employee has no membership record (not blocked, like in the grid)</param>
/// <param name="Groups">Group memberships the caller may see, filtered to the requested group when one was named</param>
/// <param name="HiddenGroupMembershipCount">Number of memberships in groups outside the caller's group scope; their names are not shown</param>
/// <param name="RequestedGroupName">Name of the group the question was limited to, null when none</param>
/// <param name="NotMemberOfRequestedGroup">True when a group was named and the employee has no membership in it at all</param>
/// <param name="Days">One status per checked day, oldest first</param>
/// <param name="CombinedGroupFrom">Earliest start of the shown group memberships, null when a membership has no lower bound or there is none</param>
/// <param name="CombinedGroupUntil">Latest end of the shown group memberships, null when a membership is open-ended or there is none</param>
public sealed record PlanningPeriodDiagnosis(
    string Person,
    MembershipWindow? Employment,
    IReadOnlyList<PlanningGroupMembershipPeriod> Groups,
    int HiddenGroupMembershipCount,
    string? RequestedGroupName,
    bool NotMemberOfRequestedGroup,
    IReadOnlyList<PlanningDayStatus> Days,
    DateOnly? CombinedGroupFrom = null,
    DateOnly? CombinedGroupUntil = null);
