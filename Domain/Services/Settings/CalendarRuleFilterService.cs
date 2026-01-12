using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Settings;

public class CalendarRuleFilterService : ICalendarRuleFilterService
{
    public IQueryable<CalendarRule> ApplyFilters(IQueryable<CalendarRule> query, CalendarRulesFilter filter)
    {
        var filteredQuery = ApplyStateCountryFilter(query, filter.List);
        return filteredQuery;
    }

    public IQueryable<CalendarRule> ApplyStateCountryFilter(IQueryable<CalendarRule> query, List<StateCountryToken> stateCountryTokens)
    {
        var selectedTokens = stateCountryTokens.Where(x => x.Select == true).ToList();

        if (!selectedTokens.Any())
        {
            return query;
        }

        var selectedCombinations = selectedTokens
            .Select(t => t.Country + "|" + t.State)
            .ToList();

        query = query.Where(cr => selectedCombinations.Contains(cr.Country + "|" + cr.State));

        return query;
    }
}