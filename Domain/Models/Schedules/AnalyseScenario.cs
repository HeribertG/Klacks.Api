// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents a what-if analysis scenario for schedule planning.
/// </summary>
/// <param name="Token">Unique token used to tag all cloned schedule data belonging to this scenario</param>
/// <param name="GroupId">The group this scenario belongs to</param>
/// <param name="FromDate">Start of the scenario time range</param>
/// <param name="UntilDate">End of the scenario time range</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Models.Schedules;

public class AnalyseScenario : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid GroupId { get; set; }

    public Group? Group { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public Guid Token { get; set; }

    public string CreatedByUser { get; set; } = string.Empty;

    public AnalyseScenarioStatus Status { get; set; } = AnalyseScenarioStatus.Active;
}
