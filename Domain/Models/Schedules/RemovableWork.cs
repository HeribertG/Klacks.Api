// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A work an absence takes away because nobody covers it.
/// </summary>
/// <param name="WorkId">The work</param>
/// <param name="ClientId">Its owner</param>
/// <param name="ShiftId">Its shift</param>
/// <param name="Date">Workday</param>
/// <param name="StartTime">Start of the work</param>
/// <param name="EndTime">End of the work</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record RemovableWork(Guid WorkId, Guid ClientId, Guid ShiftId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
