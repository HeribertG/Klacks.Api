// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single-day entry for the resource monitor dashboard, expressed as employee headcount (MA).
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="WunschCount">Desired daily readiness (rosa gepunktet): per employee min(MaxWorkDays, flaggedDays) / 7 for 24/7 contracts, 1.0 on flagged days otherwise</param>
/// <param name="MaxCount">Maximum daily readiness (rot gestrichelt): per employee min(MaxConsecutiveDays, flaggedDays) / 7 for 24/7 contracts, 1.0 on flagged days otherwise</param>
/// <param name="TotalCount">Total headcount (blau): number of distinct employees with active contracts on this date, regardless of weekday or cap</param>
/// <param name="DienstCount">Number of shifts (services) scheduled on this date</param>
/// <param name="AbsenzCount">Sum of Absence.DefaultValue per active BreakPlaceholder (literal, no FTE / no weekday filter)</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ResourceMonitorDayResource
{
    public DateOnly Date { get; set; }
    public double WunschCount { get; set; }
    public double MaxCount { get; set; }
    public double TotalCount { get; set; }
    public double DienstCount { get; set; }
    public double AbsenzCount { get; set; }
}
