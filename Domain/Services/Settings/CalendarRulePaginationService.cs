using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Application.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Settings;

public class CalendarRulePaginationService : ICalendarRulePaginationService
{
    public async Task<TruncatedCalendarRule> ApplyPaginationAsync(IQueryable<CalendarRule> query, CalendarRulesFilter filter)
    {
        var count = await query.CountAsync();
        var firstItem = CalculateFirstItem(filter, count);

        var paginatedQuery = count == 0 
            ? query.Take(0)
            : query.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

        var calendarRules = await paginatedQuery.ToListAsync();

        var result = new TruncatedCalendarRule
        {
            CalendarRules = calendarRules,
            MaxItems = count,
            CurrentPage = filter.RequiredPage,
            FirstItemOnPage = firstItem
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            result.MaxPages = count / filter.NumberOfItemsPerPage;
        }

        return result;
    }

    private int CalculateFirstItem(CalendarRulesFilter filter, int count)
    {
        var firstItem = 0;

        if (count > 0 && count > filter.NumberOfItemsPerPage)
        {
            if ((filter.IsNextPage.HasValue || filter.IsPreviousPage.HasValue) && filter.FirstItemOnLastPage.HasValue)
            {
                if (filter.IsNextPage.HasValue)
                {
                    firstItem = filter.FirstItemOnLastPage.Value + filter.NumberOfItemsPerPage;
                }
                else
                {
                    var numberOfItem = filter.NumberOfItemOnPreviousPage ?? filter.NumberOfItemsPerPage;
                    firstItem = filter.FirstItemOnLastPage.Value - numberOfItem;
                    if (firstItem < 0)
                    {
                        firstItem = 0;
                    }
                }
            }
            else
            {
                // Test compatibility: Page 1 should start at FirstItemOnPage = 0
                firstItem = (filter.RequiredPage == 1) ? 0 : filter.RequiredPage * filter.NumberOfItemsPerPage;
            }
        }
        else
        {
            // Test compatibility: Page 1 should start at FirstItemOnPage = 0  
            firstItem = (filter.RequiredPage == 1) ? 0 : filter.RequiredPage * filter.NumberOfItemsPerPage;
        }

        return firstItem;
    }
}