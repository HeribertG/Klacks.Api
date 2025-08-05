using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Interfaces.Domains;

public interface IAbsenceSortingService
{
    IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language);
}