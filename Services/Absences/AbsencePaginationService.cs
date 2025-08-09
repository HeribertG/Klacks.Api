using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Services.Absences;

public class AbsencePaginationService : IAbsencePaginationService
{
    public async Task<TruncatedAbsence> ApplyPaginationAsync(IQueryable<Absence> query, AbsenceFilter filter)
    {
        var count = await query.CountAsync();
        var firstItem = CalculateFirstItem(filter, count);

        var paginatedQuery = count == 0 
            ? query.Take(0)
            : query.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

        var absences = await paginatedQuery.ToListAsync();

        var result = new TruncatedAbsence
        {
            Absences = absences,
            MaxItems = count,
            CurrentPage = filter.RequiredPage,
            FirstItemOnPage = count <= firstItem ? -1 : firstItem
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            var maxPage = count / filter.NumberOfItemsPerPage;
            result.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        return result;
    }

    private int CalculateFirstItem(AbsenceFilter filter, int count)
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