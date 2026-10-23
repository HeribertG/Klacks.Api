// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule for which tasks belong into a container template slot, used by the available-task list and the container
/// autofill. RunsOn returns the rule as an expression; ShiftReader applies it with Where, so it runs in the database. A slot applies on the days get_shift_schedule's container_lookup gives it, and a task is a candidate when the
/// engine runs it (get_shift_schedule's task rule) on every one of those days:
/// - normal slot of a weekday (not a holiday): the task's weekday is ticked, whatever its holiday flags;
/// - holiday slot of a weekday (IsWeekdayAndHoliday, applies on public holidays falling on that weekday): IsHoliday, or the
///   weekday ticked together with IsWeekdayAndHoliday;
/// - generic holiday slot (applies on every public holiday; its weekday is only a storage key): IsHoliday, or
///   IsWeekdayAndHoliday with all seven weekdays ticked.
/// </summary>
/// <param name="weekday">Stored weekday of the slot (0=Sunday .. 6=Saturday); any other value does not restrict the weekday</param>
/// <param name="isHoliday">True for a holiday slot; false or null for the normal slot</param>
/// <param name="isWeekdayAndHoliday">Only read for a holiday slot: true = the holiday slot of the weekday, false or null = the
/// generic holiday slot</param>

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

    public static Expression<Func<Shift, bool>> RunsOn(int weekday, bool? isHoliday, bool? isWeekdayAndHoliday)
    {
        if (isHoliday != true)
        {
            return RunsOnNormalDay(weekday);
        }

        return isWeekdayAndHoliday == true
            ? RunsOnHolidayOfWeekday(weekday)
            : RunsOnEveryHoliday();
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

    private static Expression<Func<Shift, bool>> RunsOnEveryHoliday()
    {
        var shift = Expression.Parameter(typeof(Shift), ShiftParameterName);
        var everyWeekdayAndHoliday = WeekdayFlagNames
            .Select(name => (Expression)Expression.Property(shift, name))
            .Aggregate(
                (Expression)Expression.Property(shift, nameof(Shift.IsWeekdayAndHoliday)),
                Expression.AndAlso);
        var body = Expression.OrElse(Expression.Property(shift, nameof(Shift.IsHoliday)), everyWeekdayAndHoliday);
        return Expression.Lambda<Func<Shift, bool>>(body, shift);
    }

    private static MemberExpression? WeekdayTicked(ParameterExpression shift, int weekday)
    {
        return weekday >= 0 && weekday < WeekdayFlagNames.Length
            ? Expression.Property(shift, WeekdayFlagNames[weekday])
            : null;
    }
}
