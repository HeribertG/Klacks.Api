using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Absences;

public class AbsenceSortingService : IAbsenceSortingService
{
    public IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language)
    {
        if (string.IsNullOrEmpty(sortOrder))
        {
            return query;
        }

        var lang = language?.ToLower() ?? "en";
        var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy?.ToLower() switch
        {
            "name" => ApplyNameSorting(query, lang, isAscending),
            "description" => ApplyDescriptionSorting(query, lang, isAscending),
            _ => query
        };
    }

    private IQueryable<Absence> ApplyNameSorting(IQueryable<Absence> query, string language, bool isAscending)
    {
        return language switch
        {
            "de" => isAscending 
                ? query.OrderBy(x => x.Name.De!) 
                : query.OrderByDescending(x => x.Name.De!),
            "en" => isAscending 
                ? query.OrderBy(x => x.Name.En!) 
                : query.OrderByDescending(x => x.Name.En!),
            "fr" => isAscending 
                ? query.OrderBy(x => x.Name.Fr!) 
                : query.OrderByDescending(x => x.Name.Fr!),
            "it" => isAscending 
                ? query.OrderBy(x => x.Name.It!) 
                : query.OrderByDescending(x => x.Name.It!),
            _ => isAscending 
                ? query.OrderBy(x => x.Name.En!) 
                : query.OrderByDescending(x => x.Name.En!)
        };
    }

    private IQueryable<Absence> ApplyDescriptionSorting(IQueryable<Absence> query, string language, bool isAscending)
    {
        return language switch
        {
            "de" => isAscending 
                ? query.OrderBy(x => x.Description.De!) 
                : query.OrderByDescending(x => x.Description.De!),
            "en" => isAscending 
                ? query.OrderBy(x => x.Description.En!) 
                : query.OrderByDescending(x => x.Description.En!),
            "fr" => isAscending 
                ? query.OrderBy(x => x.Description.Fr!) 
                : query.OrderByDescending(x => x.Description.Fr!),
            "it" => isAscending 
                ? query.OrderBy(x => x.Description.It!) 
                : query.OrderByDescending(x => x.Description.It!),
            _ => isAscending 
                ? query.OrderBy(x => x.Description.En!) 
                : query.OrderByDescending(x => x.Description.En!)
        };
    }
}