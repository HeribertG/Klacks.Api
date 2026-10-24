// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime configuration of the silent retrieval expansion, bound from Assistant:SkillGraph at startup.
/// </summary>
/// <param name="ExplorationEpsilon">Probability of skipping the expansion on a turn. 0 switches the skip off;
/// values outside [0, 1] are clamped by the expander.</param>

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public sealed record SkillGraphExpansionOptions(double ExplorationEpsilon)
{
    public static SkillGraphExpansionOptions Default { get; } =
        new(SkillGraphConstants.ExpansionExplorationEpsilon);
}
