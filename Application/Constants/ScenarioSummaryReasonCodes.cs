// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stable codes naming why a demanded shift slot of a scenario is still open. The UI translates them; the order of
/// the constants is the order in which the classifier checks them, from the most fundamental cause to the least.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class ScenarioSummaryReasonCodes
{
    public const string NoAgentInScope = "NO_AGENT_IN_SCOPE";
    public const string NoActiveContract = "NO_ACTIVE_CONTRACT";
    public const string NoAgentWorksOnWeekday = "NO_AGENT_WORKS_ON_WEEKDAY";
    public const string NoAgentPerformsShiftWork = "NO_AGENT_PERFORMS_SHIFT_WORK";
    public const string CapacityOrRules = "CAPACITY_OR_RULES";
}
