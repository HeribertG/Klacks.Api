using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class CalendarRuleSortingService : ICalendarRuleSortingService
{
    public IQueryable<CalendarRule> ApplySorting(IQueryable<CalendarRule> query, string orderBy, string sortOrder, string language)
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
            "state" => ApplyStateSorting(query, isAscending),
            "country" => ApplyCountrySorting(query, isAscending),
            "description" => ApplyDescriptionSorting(query, lang, isAscending),
            _ => query
        };
    }

    private IQueryable<CalendarRule> ApplyNameSorting(IQueryable<CalendarRule> query, string language, bool isAscending)
    {
        return language switch
        {
            "de" => isAscending 
                ? query.OrderBy(x => x.Name!.De!) 
                : query.OrderByDescending(x => x.Name!.De!),
            "en" => isAscending 
                ? query.OrderBy(x => x.Name!.En!) 
                : query.OrderByDescending(x => x.Name!.En!),
            "fr" => isAscending 
                ? query.OrderBy(x => x.Name!.Fr!) 
                : query.OrderByDescending(x => x.Name!.Fr!),
            "it" => isAscending 
                ? query.OrderBy(x => x.Name!.It!) 
                : query.OrderByDescending(x => x.Name!.It!),
            _ => isAscending 
                ? query.OrderBy(x => x.Name!.En!) 
                : query.OrderByDescending(x => x.Name!.En!)
        };
    }

    private IQueryable<CalendarRule> ApplyStateSorting(IQueryable<CalendarRule> query, bool isAscending)
    {
        return isAscending 
            ? query.OrderBy(x => x.State) 
            : query.OrderByDescending(x => x.State);
    }

    private IQueryable<CalendarRule> ApplyCountrySorting(IQueryable<CalendarRule> query, bool isAscending)
    {
        return isAscending 
            ? query.OrderBy(x => x.Country) 
            : query.OrderByDescending(x => x.Country);
    }

    private IQueryable<CalendarRule> ApplyDescriptionSorting(IQueryable<CalendarRule> query, string language, bool isAscending)
    {
        return language switch
        {
            "de" => isAscending 
                ? query.OrderBy(x => x.Description!.De!) 
                : query.OrderByDescending(x => x.Description!.De!),
            "en" => isAscending 
                ? query.OrderBy(x => x.Description!.En!) 
                : query.OrderByDescending(x => x.Description!.En!),
            "fr" => isAscending 
                ? query.OrderBy(x => x.Description!.Fr!) 
                : query.OrderByDescending(x => x.Description!.Fr!),
            "it" => isAscending 
                ? query.OrderBy(x => x.Description!.It!) 
                : query.OrderByDescending(x => x.Description!.It!),
            _ => isAscending 
                ? query.OrderBy(x => x.Description!.En!) 
                : query.OrderByDescending(x => x.Description!.En!)
        };
    }
}