// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A slot of the absent employee that cover_absence could NOT auto-cover, with the reason
/// (no eligible candidate -&gt; under-coverage, or the absent work is locked and needs manual review).
/// </summary>
/// <param name="ShiftId">The shift that remains uncovered (original shift id)</param>
/// <param name="Date">Workday</param>
/// <param name="Reason">Why it could not be covered</param>
/// <param name="WorkId">The scenario clone of the open work; null when the work was not cloned</param>
/// <param name="StartTime">Slot start, so the UI can ask Recovery/Candidates for alternatives without re-reading the grid</param>
/// <param name="EndTime">Slot end</param>
public sealed record UncoveredSlot(
    Guid ShiftId,
    DateOnly Date,
    string Reason,
    Guid? WorkId = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null);
