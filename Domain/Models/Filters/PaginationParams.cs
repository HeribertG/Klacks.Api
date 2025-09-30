namespace Klacks.Api.Domain.Models.Filters;

public class PaginationParams
{
    public int PageIndex { get; set; } = 0;

    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; } = string.Empty;

    public bool IsDescending { get; set; } = false;

    
    public int Skip => PageIndex * PageSize;

    public int Take => PageSize;
}