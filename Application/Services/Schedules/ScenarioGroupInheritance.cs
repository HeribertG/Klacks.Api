// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The one rule for the group of a scenario a run produces: the group the run asked for, else the group of the scenario
/// it ran on. Shared by the apply that writes the scenario and the autofill presence that announces the run, so the
/// group a viewer sees the run in is the group its result lands in.
/// </summary>
public static class ScenarioGroupInheritance
{
    /// <param name="requestedGroupId">Group the run was started for; null = none requested</param>
    /// <param name="sourceScenario">Scenario the run planned on; null = the real schedule</param>
    public static Guid? Resolve(Guid? requestedGroupId, AnalyseScenario? sourceScenario)
        => requestedGroupId ?? sourceScenario?.GroupId;
}
