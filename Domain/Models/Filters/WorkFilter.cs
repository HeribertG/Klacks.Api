// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Filters;

public class WorkFilter
{
    public string SearchString { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? SelectedGroup { get; set; }

    public string OrderBy { get; set; } = "name";

    public string SortOrder { get; set; } = "asc";

    public bool ShowEmployees { get; set; } = true;

    public bool ShowExtern { get; set; } = true;

    public bool IndividualSort { get; set; }

    public int StartRow { get; set; } = 0;

    public int RowCount { get; set; } = 200;

    public int PaymentInterval { get; set; } = 2;

    /// <summary>
    /// Optional single-client scope. When set, WorkList returns only that client
    /// and skips pagination so refresh round-trips can target rows outside the
    /// default page window.
    /// </summary>
    public Guid? ClientId { get; set; }
}