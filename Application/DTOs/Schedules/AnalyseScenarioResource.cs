// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for displaying an AnalyseScenario in the frontend.
/// </summary>

namespace Klacks.Api.Application.DTOs.Schedules;

public class AnalyseScenarioResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public Guid Token { get; set; }

    /// <summary>
    /// Correlation id that groups Wizard 1/2/3 scenarios from the same test run.
    /// </summary>
    public Guid? RunGroupId { get; set; }

    public string CreatedByUser { get; set; } = string.Empty;

    public int Status { get; set; }

    /// <summary>Serialised score snapshot of the run that produced this scenario; null for a hand-made one.</summary>
    public string? SubScoreJson { get; set; }

    /// <summary>Share of cells the run moved relative to the plan it started from.</summary>
    public double? ChurnRatio { get; set; }

    /// <summary>Hard-constraint violations the produced plan still carries.</summary>
    public int? Stage0Violations { get; set; }
}
