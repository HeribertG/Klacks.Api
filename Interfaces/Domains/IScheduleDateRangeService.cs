using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.Resources.Filter;

namespace Klacks.Api.Interfaces.Domains;

public interface IScheduleDateRangeService
{
    (DateOnly startDate, DateOnly endDate) CalculateScheduleDateRange(ShiftScheduleFilter filter);
   
    IQueryable<Shift> ApplyScheduleDateFilter(IQueryable<Shift> query, DateOnly startDate, DateOnly endDate);
}