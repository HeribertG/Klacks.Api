// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request DTO for creating a new AnalyseScenario.
/// </summary>
/// <param name="Name">Name of the scenario</param>
/// <param name="GroupId">Group for which the scenario is created, or null to clone all groups in the date range</param>
/// <param name="FromDate">Start date of the scenario period</param>
/// <param name="UntilDate">End date of the scenario period</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public class CreateAnalyseScenarioRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }
}
