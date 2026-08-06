// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Keeps the background optimiser's candidates from piling up. Nobody asked for these scenarios, so a
/// newer one for the same selection replaces the older instead of standing beside it, and one that
/// nobody used within its time to live is removed.
/// </summary>
public interface IWizard4CandidateLifecycleService
{
    /// <summary>
    /// Retires an older candidate that a newer one replaces, including its cloned schedule data.
    /// </summary>
    /// <param name="oldCandidate">The candidate being replaced.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SupersedeAsync(AnalyseScenario oldCandidate, CancellationToken ct);

    /// <summary>
    /// Removes every candidate older than the time to live.
    /// </summary>
    /// <param name="nowUtc">Current time, so the caller controls the clock.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How many candidates were removed.</returns>
    Task<int> ExpireStaleCandidatesAsync(DateTime nowUtc, CancellationToken ct);
}
