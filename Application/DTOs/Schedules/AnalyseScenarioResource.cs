// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO fuer die Darstellung eines AnalyseScenarios im Frontend.
/// </summary>

namespace Klacks.Api.Application.DTOs.Schedules;

public class AnalyseScenarioResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public Guid Token { get; set; }

    public string CreatedByUser { get; set; } = string.Empty;

    public int Status { get; set; }
}
