using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Results;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Common;

public class GenericPaginationService<T> : IGenericPaginationService<T> where T : class
{
    public async Task<PagedResult<T>> ApplyPaginationAsync(IQueryable<T> query, int pageNumber, int pageSize)
    {
        var totalCount = await query.CountAsync();

        var skip = (pageNumber - 1) * pageSize;
        var items = await query.Skip(skip).Take(pageSize).ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<T>> ApplyAdvancedPaginationAsync(
        IQueryable<T> query, 
        int pageNumber, 
        int pageSize,
        bool? isNextPage = null,
        bool? isPreviousPage = null,
        int? firstItemOnLastPage = null,
        int? numberOfItemsOnPreviousPage = null)
    {
        var totalCount = await query.CountAsync();

        var firstItem = CalculateFirstItemAdvanced(
            pageNumber, 
            pageSize, 
            totalCount, 
            isNextPage, 
            isPreviousPage, 
            firstItemOnLastPage, 
            numberOfItemsOnPreviousPage);

        var items = await query.Skip(firstItem).Take(pageSize).ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public int CalculateFirstItem(int pageNumber, int pageSize, int totalCount)
    {
        if (totalCount == 0)
        {
            return 0;
        }

        var firstItem = (pageNumber - 1) * pageSize;
        return Math.Min(firstItem, totalCount - 1);
    }

    public async Task<PaginationMetadata> GetPaginationMetadataAsync(IQueryable<T> query, int pageNumber, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        var firstItem = CalculateFirstItem(pageNumber, pageSize, totalCount);

        return new PaginationMetadata
        {
            TotalCount = totalCount,
            TotalPages = totalPages,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FirstItemOnPage = totalCount <= firstItem ? -1 : firstItem,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }

    private int CalculateFirstItemAdvanced(
        int pageNumber, 
        int pageSize, 
        int totalCount,
        bool? isNextPage,
        bool? isPreviousPage,
        int? firstItemOnLastPage,
        int? numberOfItemsOnPreviousPage)
    {
        if (totalCount == 0)
        {
            return 0;
        }

        if (totalCount <= pageSize)
        {
            return 0;
        }

        if ((isNextPage.HasValue || isPreviousPage.HasValue) && firstItemOnLastPage.HasValue)
        {
            if (isNextPage.HasValue && isNextPage.Value)
            {
                return firstItemOnLastPage.Value + pageSize;
            }
            
            if (isPreviousPage.HasValue && isPreviousPage.Value)
            {
                var numberOfItems = numberOfItemsOnPreviousPage ?? pageSize;
                var firstItem = firstItemOnLastPage.Value - numberOfItems;
                return Math.Max(firstItem, 0);
            }
        }

        return (pageNumber - 1) * pageSize;
    }
}