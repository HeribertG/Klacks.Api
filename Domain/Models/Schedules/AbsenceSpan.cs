// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One absence of one person on one day, entered or about to be entered.
/// </summary>
/// <param name="ClientId">The absent person</param>
/// <param name="AbsenceId">Absence type</param>
/// <param name="Date">Day of the absence</param>
/// <param name="StartTime">Start of the absence</param>
/// <param name="EndTime">End of the absence</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record AbsenceSpan(Guid ClientId, Guid AbsenceId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
