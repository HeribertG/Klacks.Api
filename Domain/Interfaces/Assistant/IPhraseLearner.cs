// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One round of phrase learning for a single cluster: generate wordings, activate them one at a time and
/// let the routing oracle decide. Separated from the loop because the loop decides which clusters are
/// worth a round and this decides what a round is.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPhraseLearner
{
    Task<PhraseLearningOutcome> LearnAsync(
        SkillLearningClusterContext cluster,
        string targetSkill,
        CancellationToken cancellationToken = default);
}
