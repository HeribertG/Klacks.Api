// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The seven weekday flags a shift carries, as one value: read from a shift, built from a set of ISO weekdays (1=Monday..
/// 7=Sunday), and listed back as ISO weekdays. The one place that maps between the flags and the weekday numbers.
/// </summary>
/// <param name="Monday">Runs on Mondays</param>
/// <param name="Tuesday">Runs on Tuesdays</param>
/// <param name="Wednesday">Runs on Wednesdays</param>
/// <param name="Thursday">Runs on Thursdays</param>
/// <param name="Friday">Runs on Fridays</param>
/// <param name="Saturday">Runs on Saturdays</param>
/// <param name="Sunday">Runs on Sundays</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public readonly record struct WeekdayFlags(
    bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday)
{
    /// <param name="shift">The shift whose flags are read</param>
    public static WeekdayFlags Of(Shift shift) => new(
        shift.IsMonday, shift.IsTuesday, shift.IsWednesday, shift.IsThursday, shift.IsFriday, shift.IsSaturday, shift.IsSunday);

    /// <param name="isoWeekdays">ISO weekdays 1..7 that are set</param>
    public static WeekdayFlags OfDays(IEnumerable<int> isoWeekdays)
    {
        var days = isoWeekdays.ToHashSet();
        return new WeekdayFlags(
            days.Contains(IsoWeekday.Monday),
            days.Contains(IsoWeekday.Tuesday),
            days.Contains(IsoWeekday.Wednesday),
            days.Contains(IsoWeekday.Thursday),
            days.Contains(IsoWeekday.Friday),
            days.Contains(IsoWeekday.Saturday),
            days.Contains(IsoWeekday.Sunday));
    }

    /// <summary>
    /// The set weekdays as ISO weekdays in ascending order.
    /// </summary>
    public IReadOnlyList<int> IsoDays()
    {
        var days = new List<int>();
        AddIf(days, Monday, IsoWeekday.Monday);
        AddIf(days, Tuesday, IsoWeekday.Tuesday);
        AddIf(days, Wednesday, IsoWeekday.Wednesday);
        AddIf(days, Thursday, IsoWeekday.Thursday);
        AddIf(days, Friday, IsoWeekday.Friday);
        AddIf(days, Saturday, IsoWeekday.Saturday);
        AddIf(days, Sunday, IsoWeekday.Sunday);
        return days;
    }

    private static void AddIf(List<int> days, bool isSet, int isoDay)
    {
        if (isSet)
        {
            days.Add(isoDay);
        }
    }
}
