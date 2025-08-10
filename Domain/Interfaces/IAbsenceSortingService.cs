using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IAbsenceSortingService
{
    IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language);
}