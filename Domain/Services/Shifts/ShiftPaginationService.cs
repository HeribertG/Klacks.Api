using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftPaginationService : IShiftPaginationService
{
    public async Task<TruncatedShift> ApplyPaginationAsync(IQueryable<Shift> filteredQuery, ShiftFilter filter)
    {
        var count = await filteredQuery.CountAsync();
        var maxPage = filter.NumberOfItemsPerPage > 0 ? (count / filter.NumberOfItemsPerPage) : 0;
        var firstItem = CalculateFirstItem(filter, count);

        var shifts = count == 0
            ? new List<Shift>()
            : await filteredQuery.Skip(firstItem).Take(filter.NumberOfItemsPerPage).ToListAsync();

        var result = new TruncatedShift
        {
            Shifts = shifts,
            MaxItems = count,
            CurrentPage = filter.RequiredPage,
            FirstItemOnPage = count <= firstItem ? -1 : firstItem
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            result.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        return result;
    }

    public int CalculateFirstItem(ShiftFilter filter, int totalCount)
    {
        var firstItem = 0;

        if (totalCount > 0 && totalCount > filter.NumberOfItemsPerPage)
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
                firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
            }
        }
        else
        {
            firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
        }

        return firstItem;
    }
}