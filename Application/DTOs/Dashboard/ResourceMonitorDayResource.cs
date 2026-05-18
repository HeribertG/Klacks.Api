// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single-day entry for the resource monitor dashboard, expressed as employee headcount.
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="MaxKapazitaetCount">Number of employees with active contracts who work on this weekday</param>
/// <param name="DienstCount">Number of shifts (services) scheduled on this date</param>
/// <param name="AbsenzCount">Sum of Absence.DefaultValue weights for employees absent on this date (1.0 = full, 0.5 = half)</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ResourceMonitorDayResource
{
    public DateOnly Date { get; set; }
    public double MaxKapazitaetCount { get; set; }
    public double DienstCount { get; set; }
    public double AbsenzCount { get; set; }
}
