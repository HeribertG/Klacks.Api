// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Results;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    public bool HasNextPage => PageNumber < TotalPages;
    
    public bool HasPreviousPage => PageNumber > 1;
    
    public int? FirstItemOnLastPage { get; set; }
    
    public bool? IsNextPage { get; set; }
    
    public bool? IsPreviousPage { get; set; }
    
    public int? NumberOfItemOnPreviousPage { get; set; }
}