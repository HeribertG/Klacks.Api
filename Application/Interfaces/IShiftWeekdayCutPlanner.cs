// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Resolves a split_shifts_by_weekday call (group name, plannable shifts, visibility, skip reasons) into the shifts it
/// would cut without writing anything, so the skill and its confirmation preview provider work from the very same selection.
/// </summary>
public interface IShiftWeekdayCutPlanner
{
    Task<ShiftWeekdayCutSelection> PlanAsync(string groupName, CancellationToken cancellationToken = default);
}
