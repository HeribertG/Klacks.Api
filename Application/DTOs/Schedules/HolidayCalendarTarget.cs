// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The holiday calendar a holiday question is about: its display name, which path answered, and its calculator per
/// calendar year (null when no calendar is configured).
/// </summary>
/// <param name="Label">Display name of the calendar, never an id</param>
/// <param name="Kind">Which path answered, see HolidayCalendarTargetKinds</param>
/// <param name="CalculatorForYear">Calculator for one calendar year</param>

using Klacks.Api.Domain.Interfaces.CalendarSelections;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record HolidayCalendarTarget(
    string? Label,
    string Kind,
    Func<int, Task<IHolidaysListCalculator?>> CalculatorForYear);
