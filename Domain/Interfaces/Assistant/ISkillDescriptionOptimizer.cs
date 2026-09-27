// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generates ProposedSkillChange records by analyzing recent corrected trajectories where the LLM
/// picked the wrong skill, and asking a cheap LLM to suggest a tighter description for the wrongly
/// picked skill so it stops matching such queries.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillDescriptionOptimizer
{
    Task<SkillDescriptionOptimizerResult> GenerateProposalsAsync(
        int maxTrajectoriesToAnalyze, CancellationToken cancellationToken = default);
}
