using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IScheduleDateRangeService
{
    (DateOnly startDate, DateOnly endDate) CalculateScheduleDateRange(ShiftScheduleFilter filter);
   
    IQueryable<Shift> ApplyScheduleDateFilter(IQueryable<Shift> query, DateOnly startDate, DateOnly endDate);
}