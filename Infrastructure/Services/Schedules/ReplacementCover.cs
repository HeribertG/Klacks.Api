// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// One replacement: part or all of a work handed to a substitute by a WorkChange of a replacement type.
/// </summary>
/// <param name="WorkId">The work carrying the replacement; it must not be moved or deleted by a wizard</param>
/// <param name="OriginalClientId">The person the work belongs to</param>
/// <param name="SubstituteClientId">The person working the replaced span (WorkChange.ReplaceClientId)</param>
/// <param name="ShiftId">Shift of the work</param>
/// <param name="Date">Calendar date of the work</param>
/// <param name="Start">Effective start of the replaced span (see <see cref="ReplacementWindow"/>)</param>
/// <param name="End">Effective end of the replaced span</param>
/// <param name="StartAt">Replaced span placed on the calendar (after midnight of a night shift: next day)</param>
/// <param name="EndAt">End of the replaced span on the calendar</param>
/// <param name="Hours">Hours of the replaced span (WorkChange.ChangeTime)</param>
public sealed record ReplacementCover(
    Guid WorkId,
    Guid OriginalClientId,
    Guid SubstituteClientId,
    Guid ShiftId,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    DateTime StartAt,
    DateTime EndAt,
    decimal Hours);
