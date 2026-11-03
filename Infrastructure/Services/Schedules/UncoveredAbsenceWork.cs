// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// A work as <see cref="UncoveredAbsenceEntries"/> needs it to speak for the correction, briefing and travel blocks
/// around it: a work handed over completely leaves no Work block in the timeline, so its verdict and its labels come
/// from here and not from a block.
/// </summary>
/// <param name="ShiftId">Shift of the work, named in the notice</param>
/// <param name="Date">Day of the work, the day the notice is dated on</param>
/// <param name="Service">Company-local interval of the work as entered, the time substitutes can take over</param>
public sealed record UncoveredAbsenceWork(Guid ShiftId, DateOnly Date, ReplacementInterval Service);
