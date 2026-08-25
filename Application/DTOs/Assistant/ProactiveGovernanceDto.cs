// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ProactiveGovernanceDto
{
    public bool KillSwitchActive { get; set; }

    public List<ProactiveGovernanceRuleDto> Rules { get; set; } = new();
}
