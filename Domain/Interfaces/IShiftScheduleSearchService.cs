using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftScheduleSearchService
{
    IQueryable<ShiftDayAssignment> ApplySearchFilter(IQueryable<ShiftDayAssignment> query, string? searchString);
}
