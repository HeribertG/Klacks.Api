using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftSortingService
{
    IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder);
}