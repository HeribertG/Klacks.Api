using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Domain.Interfaces;

public interface IGenericPaginationService<T> where T : class
{
    Task<PagedResult<T>> ApplyPaginationAsync(IQueryable<T> query, int pageNumber, int pageSize);

    Task<PagedResult<T>> ApplyAdvancedPaginationAsync(
        IQueryable<T> query, 
        int pageNumber, 
        int pageSize,
        bool? isNextPage = null,
        bool? isPreviousPage = null,
        int? firstItemOnLastPage = null,
        int? numberOfItemsOnPreviousPage = null);

    int CalculateFirstItem(int pageNumber, int pageSize, int totalCount);

    Task<PaginationMetadata> GetPaginationMetadataAsync(IQueryable<T> query, int pageNumber, int pageSize);
}

public class PaginationMetadata
{
    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int FirstItemOnPage { get; set; }

    public bool HasPreviousPage { get; set; }

    public bool HasNextPage { get; set; }
}