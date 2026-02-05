using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface ICalendarRuleFilterService
{
    IQueryable<CalendarRule> ApplyFilters(IQueryable<CalendarRule> query, CalendarRulesFilter filter);
    IQueryable<CalendarRule> ApplyStateCountryFilter(IQueryable<CalendarRule> query, List<StateCountryToken> stateCountryTokens);
}