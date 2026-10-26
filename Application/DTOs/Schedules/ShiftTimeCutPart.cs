// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One part of a planned time-of-day cut (cut_shift).
/// </summary>
/// <param name="Id">Id the part will have: the plannable shift itself for the first part, a fresh id otherwise</param>
/// <param name="Index">Zero-based position of the part in the request</param>
/// <param name="Name">Name of the part (requested or derived from the order name and the times)</param>
/// <param name="Start">Start time of the part</param>
/// <param name="End">End time of the part (before Start when the part crosses midnight)</param>
/// <param name="WorkTime">Hours of the part</param>
/// <param name="CuttingAfterMidnight">True when the part crosses midnight</param>
/// <param name="Converted">True for the first part, which reuses the plannable shift instead of creating a new one</param>
namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record ShiftTimeCutPart(
    Guid Id,
    int Index,
    string Name,
    TimeOnly Start,
    TimeOnly End,
    decimal WorkTime,
    bool CuttingAfterMidnight,
    bool Converted);
