// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One round of capability learning for a single cluster: compose recipes from existing skills, judge
/// each against both oracles, and activate the first that survives. The counterpart of IPhraseLearner -
/// the loop decides which wishes deserve a round, this decides what a round is.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ICapabilityLearner
{
    /// <summary>
    /// Composes and judges capability variants for one cluster.
    /// </summary>
    /// <param name="cluster">The wish, its language and the error the previous round recorded</param>
    /// <param name="candidateSkills">Skills the live retrieval offers for this wish; the pool the composition is drawn from</param>
    Task<CapabilityLearningOutcome> LearnAsync(
        SkillLearningClusterContext cluster,
        IReadOnlyList<string> candidateSkills,
        CancellationToken cancellationToken = default);
}
