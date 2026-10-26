// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Plans a time-of-day cut (cut_shift) of one plannable shift without writing anything, so the skill and its confirmation
/// preview work from the very same plan.
/// </summary>
public interface IShiftTimeCutPlanner
{
    Task<ShiftTimeCutPlan> PlanAsync(
        Guid shiftId,
        string partsRaw,
        string partNamesRaw,
        string? userLanguage,
        CancellationToken cancellationToken = default);
}
