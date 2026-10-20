// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The period in which an employee belongs to one group (the GroupItem validity), as the schedule grid of that group
/// uses it to lock the days outside.
/// </summary>
/// <param name="GroupId">Group the membership belongs to</param>
/// <param name="GroupName">Display name of the group</param>
/// <param name="From">First member day, null when the membership has no lower bound</param>
/// <param name="Until">Last member day, null for an open end</param>
public sealed record PlanningGroupMembershipPeriod(Guid GroupId, string GroupName, DateOnly? From, DateOnly? Until);
