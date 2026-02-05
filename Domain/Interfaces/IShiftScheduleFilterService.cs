using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftScheduleFilterService
{
    IQueryable<ShiftDayAssignment> ApplyAllFilters(IQueryable<ShiftDayAssignment> query, ShiftScheduleFilter filter);
}
