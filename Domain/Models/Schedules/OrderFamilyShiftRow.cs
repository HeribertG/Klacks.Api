// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// The columns find_split_shift_candidates reads of a sealed order, plannable shift or split part, projected in the
/// database instead of loading the whole shift.
/// </summary>
/// <param name="Id">Id of the shift</param>
/// <param name="Name">Name of the shift</param>
/// <param name="Abbreviation">Abbreviation of the shift</param>
/// <param name="Status">SealedOrder, OriginalShift or SplitShift</param>
/// <param name="StartShift">Start time</param>
/// <param name="EndShift">End time</param>
/// <param name="FromDate">First day</param>
/// <param name="UntilDate">Last day; null = open-ended</param>
/// <param name="OriginalId">The order the shift descends from; null for the sealed order itself</param>
public sealed record OrderFamilyShiftRow(
    Guid Id,
    string Name,
    string Abbreviation,
    ShiftStatus Status,
    TimeOnly StartShift,
    TimeOnly EndShift,
    DateOnly FromDate,
    DateOnly? UntilDate,
    Guid? OriginalId);
