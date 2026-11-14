// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime configuration of the skill-graph association, bound from Assistant:SkillGraph at startup.
/// </summary>
/// <param name="ExplorationEpsilon">Probability of skipping the association on a turn. 0 switches the skip off;
/// values outside [0, 1] are clamped by the expander.</param>
/// <param name="AssociationSlots">How many of the last retrieval places graph neighbours compete for; 0 switches the
/// association off, negative values count as 0.</param>

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public sealed record SkillGraphExpansionOptions(double ExplorationEpsilon, int AssociationSlots)
{
    public static SkillGraphExpansionOptions Default { get; } =
        new(SkillGraphConstants.ExpansionExplorationEpsilon, SkillGraphConstants.AssociationSlots);
}
