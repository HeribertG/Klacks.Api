// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for the client availability client list with search functionality.
/// </summary>
/// <param name="SearchString">Search term for client name/first name/company</param>
/// <param name="SelectedGroup">Optional group ID for filtering</param>
/// <param name="StartRow">Start index for paging</param>
/// <param name="RowCount">Number of results per page</param>
namespace Klacks.Api.Application.DTOs.Filter;

public class ClientAvailabilityClientFilter
{
    public string SearchString { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? SelectedGroup { get; set; }
    public string OrderBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public bool ShowEmployees { get; set; } = true;
    public bool ShowExtern { get; set; } = true;
    public int StartRow { get; set; } = 0;
    public int RowCount { get; set; } = 200;
}
