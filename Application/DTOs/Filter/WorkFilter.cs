// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Filter;

public class WorkFilter
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string OrderBy { get; set; } = string.Empty;

    public string SearchString { get; set; } = string.Empty;

    public string SortOrder { get; set; } = string.Empty;

    public List<WorkResource> Works { get; set; } = new List<WorkResource>();

    public Guid? SelectedGroup { get; set; }
}
