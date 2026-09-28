// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
