// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single-day entry for the resource monitor dashboard.
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="ShouldHours">Sum of target hours from all active contracts for this day</param>
/// <param name="ActualHours">Sum of Work.WorkTime entries for this day</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ResourceMonitorDayResource
{
    public DateOnly Date { get; set; }
    public double ShouldHours { get; set; }
    public double ActualHours { get; set; }
}
