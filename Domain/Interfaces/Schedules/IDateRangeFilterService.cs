// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IDateRangeFilterService
{
    /// <summary>
    /// Filters shifts by date range relative to <paramref name="today"/> (the company's own local day,
    /// per ICompanyClock - never the server's UTC day).
    /// </summary>
    IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange, DateOnly today);
}