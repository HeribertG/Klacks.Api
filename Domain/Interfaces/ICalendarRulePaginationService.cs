using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface ICalendarRulePaginationService
{
    Task<TruncatedCalendarRule> ApplyPaginationAsync(IQueryable<CalendarRule> query, CalendarRulesFilter filter);
}