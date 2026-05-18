// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single-day entry for the resource monitor dashboard.
/// </summary>
/// <param name="Date">The calendar date</param>
/// <param name="MaxKapazitaetHours">Sum of target hours from all active contracts for this day</param>
/// <param name="DienstHours">Sum of Work.WorkTime for regular (non-time-range, non-sporadic) shifts on this day</param>
/// <param name="AbsenzHours">Sum of Break.WorkTime entries (absences) for this day</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ResourceMonitorDayResource
{
    public DateOnly Date { get; set; }
    public double MaxKapazitaetHours { get; set; }
    public double DienstHours { get; set; }
    public double AbsenzHours { get; set; }
}
