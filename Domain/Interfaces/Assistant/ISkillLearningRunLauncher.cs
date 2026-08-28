// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts learning runs and makes sure only one runs at a time inside this process, so the six-hourly
/// tick and an administrator's manual trigger cannot overlap. Across instances the per-cluster
/// compare-and-swap claim carries that guarantee instead: two runs on two machines would work on
/// disjoint clusters, never on the same one.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningRunLauncher
{
    /// <summary>
    /// Runs to completion, or reports that a run was already under way. Used by the background tick.
    /// </summary>
    Task<SkillLearningRunTicket> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a run in the background and returns as soon as it is under way. Used by the manual
    /// endpoint, which must not hold an HTTP request open for the length of a run.
    /// </summary>
    SkillLearningRunTicket StartDetached();
}
