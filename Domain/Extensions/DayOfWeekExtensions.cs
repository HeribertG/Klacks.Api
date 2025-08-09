using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Extensions;

public static class DayOfWeekExtensions
{
    public static ShiftDayType ToShiftDayType(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => ShiftDayType.Monday,
            DayOfWeek.Tuesday => ShiftDayType.Tuesday,
            DayOfWeek.Wednesday => ShiftDayType.Wednesday,
            DayOfWeek.Thursday => ShiftDayType.Thursday,
            DayOfWeek.Friday => ShiftDayType.Friday,
            DayOfWeek.Saturday => ShiftDayType.Saturday,
            DayOfWeek.Sunday => ShiftDayType.Sunday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
        };
    }
}
