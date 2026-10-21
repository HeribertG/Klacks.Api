// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Restricts shifts to those that run on the day a container template (or a container work) is filled for, by the same
/// rule the schedule engine (get_shift_schedule) applies: on a normal day a shift runs when its weekday is ticked,
/// whatever its holiday flags; on a public holiday it runs when IsHoliday is set or when its weekday is ticked together
/// with IsWeekdayAndHoliday. The FT template of a container applies on every public holiday whatever its weekday, so only
/// IsHoliday shifts run on all of its days.
/// </summary>
/// <param name="weekday">Stored weekday (0=Sunday .. 6=Saturday); any other value does not restrict the weekday</param>
/// <param name="isHoliday">True for a public holiday; false or null for a normal day</param>
/// <param name="isWeekdayAndHoliday">Only read on a holiday: false = the FT template of every holiday (the weekday is
/// ignored), true or null = a holiday falling on the weekday</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ContainerTemplates;

public static class ShiftDayActivityFilter
{
    private const string ShiftParameterName = "shift";

    private static readonly string[] WeekdayFlagNames =
    [
        nameof(Shift.IsSunday),
        nameof(Shift.IsMonday),
        nameof(Shift.IsTuesday),
        nameof(Shift.IsWednesday),
        nameof(Shift.IsThursday),
        nameof(Shift.IsFriday),
        nameof(Shift.IsSaturday)
    ];

    public static IQueryable<Shift> Apply(IQueryable<Shift> query, int weekday, bool? isHoliday, bool? isWeekdayAndHoliday)
    {
        if (isHoliday != true)
        {
            return query.Where(RunsOnNormalDay(weekday));
        }

        if (isWeekdayAndHoliday == false)
        {
            return query.Where(s => s.IsHoliday);
        }

        return query.Where(RunsOnHolidayOfWeekday(weekday));
    }

    private static Expression<Func<Shift, bool>> RunsOnNormalDay(int weekday)
    {
        var shift = Expression.Parameter(typeof(Shift), ShiftParameterName);
        var weekdayTicked = (Expression?)WeekdayTicked(shift, weekday) ?? Expression.Constant(true);
        return Expression.Lambda<Func<Shift, bool>>(weekdayTicked, shift);
    }

    private static Expression<Func<Shift, bool>> RunsOnHolidayOfWeekday(int weekday)
    {
        var shift = Expression.Parameter(typeof(Shift), ShiftParameterName);
        var weekdayAndHoliday = Expression.Property(shift, nameof(Shift.IsWeekdayAndHoliday));
        var weekdayTicked = WeekdayTicked(shift, weekday);
        var weekdayRunsOnHoliday = weekdayTicked is null
            ? (Expression)weekdayAndHoliday
            : Expression.AndAlso(weekdayTicked, weekdayAndHoliday);
        var body = Expression.OrElse(Expression.Property(shift, nameof(Shift.IsHoliday)), weekdayRunsOnHoliday);
        return Expression.Lambda<Func<Shift, bool>>(body, shift);
    }

    private static MemberExpression? WeekdayTicked(ParameterExpression shift, int weekday)
    {
        return weekday >= 0 && weekday < WeekdayFlagNames.Length
            ? Expression.Property(shift, WeekdayFlagNames[weekday])
            : null;
    }
}
