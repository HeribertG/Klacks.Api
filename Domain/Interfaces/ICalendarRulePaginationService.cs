using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface ICalendarRulePaginationService
{
    Task<TruncatedCalendarRule> ApplyPaginationAsync(IQueryable<CalendarRule> query, CalendarRulesFilter filter);
}