using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Shifts;

public class ScheduleDateRangeService : IScheduleDateRangeService
{
    public (DateOnly startDate, DateOnly endDate) CalculateScheduleDateRange(ShiftScheduleFilter filter)
    {
        var startDateTime = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1)
            .AddDays(filter.DayVisibleAfterMonth * -1);
            
        var endDateTime = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1)
            .AddMonths(1).AddDays(-1)
            .AddDays(filter.DayVisibleAfterMonth);

        return (DateOnly.FromDateTime(startDateTime), DateOnly.FromDateTime(endDateTime));
    }

    public IQueryable<Shift> ApplyScheduleDateFilter(IQueryable<Shift> query, DateOnly startDate, DateOnly endDate)
    {
        return query.Where(shift =>
            (shift.FromDate >= startDate && shift.FromDate <= endDate) ||
            (shift.UntilDate >= startDate && shift.UntilDate <= endDate) ||
            (shift.FromDate <= startDate && shift.UntilDate >= endDate))
            .OrderBy(shift => shift.FromDate)
            .ThenBy(shift => shift.UntilDate);
    }
}