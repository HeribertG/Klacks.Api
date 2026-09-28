// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of the two grouping feasibility skills, shared by the skill attributes, the verification
/// exception and the tests so the names exist exactly once.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GroupingSkillNames
{
    public const string Analyze = "analyze_grouping_feasibility";
    public const string Apply = "apply_grouping_plan";
}
