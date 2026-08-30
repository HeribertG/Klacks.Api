// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ProactiveGovernanceDto
{
    public bool KillSwitchActive { get; set; }

    /// <summary>The installation-wide autonomy level (0-3) that caps every rule's MaxAction.</summary>
    public int GlobalAutonomyLevel { get; set; }

    /// <summary>The ProactiveMaxAction ceiling that level maps to (0=Hint, 1=Prepare, 2/3=Execute).</summary>
    public int GlobalAutonomyCap { get; set; }

    public List<ProactiveGovernanceRuleDto> Rules { get; set; } = new();
}
