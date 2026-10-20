// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One employee the schedule shows for a group, with the two periods that darken or colour the grid: the employment
/// window and the combined validity of the memberships in exactly the selected group.
/// </summary>
/// <param name="ClientId">The employee</param>
/// <param name="Employment">Employment window (entry and exit)</param>
/// <param name="GroupFrom">Earliest start of the memberships in the selected group, null when one of them has no lower bound</param>
/// <param name="GroupUntil">Latest end of the memberships in the selected group, null when one of them is open-ended</param>
/// <param name="HasGroupItem">False when the employee has no membership in exactly the selected group (for example only in a subgroup), so the grid has no group period to lock by</param>
public sealed record PlanningGroupMember(
    Guid ClientId, MembershipWindow Employment, DateOnly? GroupFrom, DateOnly? GroupUntil, bool HasGroupItem);
