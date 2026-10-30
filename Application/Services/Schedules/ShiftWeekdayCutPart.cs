// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One part of a weekday cut: the plannable shift cut down to a single weekday.
/// </summary>
/// <param name="Id">Id of the part; the plannable shift's own id for the part that reuses it</param>
/// <param name="SourceShiftId">The plannable shift the part is cut from</param>
/// <param name="IsoWeekday">The one weekday the part runs on, 1=Monday..7=Sunday</param>
/// <param name="Converted">True for the part that reuses the plannable shift (an update); false for a new part</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ShiftWeekdayCutPart(Guid Id, Guid SourceShiftId, int IsoWeekday, bool Converted);
