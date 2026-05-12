// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Filter;

public class WorkScheduleFilter
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Core payment-period start (without DayVisibleBefore padding). Used for period-hours calculation.
    /// Falls back to <see cref="StartDate"/> when not provided.
    /// </summary>
    public DateOnly? PeriodStartDate { get; set; }

    /// <summary>
    /// Core payment-period end (without DayVisibleAfter padding). Used for period-hours calculation.
    /// Falls back to <see cref="EndDate"/> when not provided.
    /// </summary>
    public DateOnly? PeriodEndDate { get; set; }

    public Guid? SelectedGroup { get; set; }

    /// <summary>
    /// Optional single-client scope for refresh round-trips (Break / Work / Expenses
    /// CRUD post-save). When set, the handler skips pagination and returns only that
    /// client's schedule entries so a client outside the first <see cref="RowCount"/>
    /// page does not silently drop out of the refresh.
    /// </summary>
    public Guid? ClientId { get; set; }
    public string OrderBy { get; set; } = "name";
    public string SortOrder { get; set; } = "asc";
    public bool ShowEmployees { get; set; } = true;
    public bool ShowExtern { get; set; } = true;
    public bool IndividualSort { get; set; }
    public int StartRow { get; set; } = 0;
    public int RowCount { get; set; } = 200;
    public int PaymentInterval { get; set; } = 2;
    public string SearchString { get; set; } = string.Empty;
    public Guid? AnalyseToken { get; set; }
}
