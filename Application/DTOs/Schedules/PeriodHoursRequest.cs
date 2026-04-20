// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class PeriodHoursRequest
{
    public List<Guid> ClientIds { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    /// <summary>
    /// Scenario scope. Null means the production schedule (original).
    /// </summary>
    public Guid? AnalyseToken { get; set; }
}
