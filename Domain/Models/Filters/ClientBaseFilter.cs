// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Gemeinsamer Filter für Client-Listen (Schedule, Availability, Absence).
/// </summary>
/// <param name="StartDate">Beginn des sichtbaren Zeitraums</param>
/// <param name="EndDate">Ende des sichtbaren Zeitraums</param>
namespace Klacks.Api.Domain.Models.Filters;

public class ClientBaseFilter
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string SearchString { get; set; } = string.Empty;

    public Guid? SelectedGroup { get; set; }

    public bool ShowEmployees { get; set; } = true;

    public bool ShowExtern { get; set; } = true;

    public string OrderBy { get; set; } = "name";

    public string SortOrder { get; set; } = "asc";

    public string? HoursSortOrder { get; set; }

    public int StartRow { get; set; } = 0;

    public int RowCount { get; set; } = 200;
}
