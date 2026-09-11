// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Refreshes every structure derived from the skill catalogue after it changed.
/// </summary>
public interface ISkillCatalogRefresher
{
    /// <summary>
    /// Refreshes the skill cache and registry, then schedules the knowledge index sync in the
    /// background and returns without waiting for it. For callers inside an HTTP request: a new
    /// language can take a full re-embed of several hundred entries.
    /// </summary>
    /// <param name="reason">Short description of the catalogue change, used in log lines and the sync status</param>
    /// <param name="cancellationToken">Cancels the registry refresh; the scheduled sync is not affected</param>
    Task RefreshAsync(string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the skill cache and registry, then waits until a knowledge index sync that started
    /// after the call has finished. Required for every caller that measures routing right after the
    /// change (learning oracles, regression gates): those read the index, and a probe against the
    /// pre-change index judges the old catalogue. Still returns normally when that sync fails.
    /// </summary>
    /// <param name="reason">Short description of the catalogue change, used in log lines and the sync status</param>
    /// <param name="cancellationToken">Stops waiting for the sync; the sync itself continues</param>
    Task RefreshAndWaitForIndexAsync(string reason, CancellationToken cancellationToken = default);
}
