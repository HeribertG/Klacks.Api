// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one biweekly pay-period rule: a fixed 14-day cycle anchored on Monday 2025-12-29 (start of ISO week 1 of 2026),
/// moved back to the configured week-start day. No ISO week numbers are involved, so the 53-week ISO year 2026 does
/// not produce overlapping periods. With a Monday week start the result equals the former ISO-parity rule for every
/// day from 2021-01-04 to 2027-01-03. Used by PeriodHoursService and the assistant's period boundaries; mirrored in
/// the Ui by CalendarUtilService.getBiweeklyPeriod with the same anchor.
/// </summary>
/// <param name="date">Any day of the wanted period</param>
/// <param name="weekStartDay">The configured first day of the week (IWeekConfiguration, Monday when unconfigured)</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Schedules;

public static class BiweeklyPayPeriod
{
    public static readonly DateOnly Anchor = new(2025, 12, 29);

    public const int CycleDays = CalendarConstants.DaysPerFortnight;

    public static (DateOnly Start, DateOnly End) Containing(DateOnly date, DayOfWeek weekStartDay)
    {
        var weekStart = MoveBackTo(date, weekStartDay);
        var anchorWeekStart = MoveBackTo(Anchor, weekStartDay);
        var start = weekStart.AddDays(-FloorMod(weekStart.DayNumber - anchorWeekStart.DayNumber, CycleDays));
        return (start, start.AddDays(CycleDays - 1));
    }

    private static DateOnly MoveBackTo(DateOnly date, DayOfWeek weekStartDay)
    {
        var offset = FloorMod((int)date.DayOfWeek - (int)weekStartDay, CalendarConstants.DaysPerWeek);
        return date.AddDays(-offset);
    }

    private static int FloorMod(int value, int modulus)
    {
        return ((value % modulus) + modulus) % modulus;
    }
}
