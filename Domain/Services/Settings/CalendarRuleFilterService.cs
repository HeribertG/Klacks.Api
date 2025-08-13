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
        var filteredStateList = stateCountryTokens.Where(x => x.Select == true).Select(x => x.State).ToList();
        var filteredCountryList = stateCountryTokens.Where(x => x.Select == true).Select(x => x.Country).Distinct().ToList();
        filteredStateList.AddRange(filteredCountryList.ToArray());

        query = query.Where(st => filteredStateList.Contains(st.State));
        query = query.Where(co => filteredCountryList.Contains(co.Country));

        return query;
    }
}