using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftScheduleSearchService
{
    IQueryable<ShiftDayAssignment> ApplySearchFilter(IQueryable<ShiftDayAssignment> query, string? searchString);
}
