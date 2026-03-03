// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Filter;

public class BreakFilter
{
    public List<AbsenceTokenFilter> Absences { get; set; } = new List<AbsenceTokenFilter>();

    public int CurrentYear { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public Guid? SelectedGroup { get; set; }

    public int? StartRow { get; set; }

    public int? RowCount { get; set; }

    public bool ShowEmployees { get; set; } = true;

    public bool ShowExtern { get; set; } = true;

    public string? HoursSortOrder { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
