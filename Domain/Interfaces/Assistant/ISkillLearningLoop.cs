// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One pass of the learning loop: claim ready clusters, classify them, try to close the phrase gaps and
/// apply the description proposals that survive the regression gate.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningLoop
{
    /// <summary>
    /// Runs one pass. The trigger tells the scheduled tick from an explicit start; in Gate only an explicit start
    /// measures description proposals.
    /// </summary>
    Task<SkillLearningRunSummary> RunAsync(
        SkillLearningRunTrigger trigger, CancellationToken cancellationToken = default);
}
