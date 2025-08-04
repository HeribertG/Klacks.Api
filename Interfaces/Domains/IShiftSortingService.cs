using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Interfaces.Domains;

public interface IShiftSortingService
{
    IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder);
}