// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single place that decides whether the caller may work in the scenario an analyse token selects. A null token is
/// the main plan and always allowed; a token of a missing, deleted or group-hidden scenario is answered exactly like
/// a token that does not exist. Admins and background callers without a user are unrestricted and never refused.
/// </summary>
public interface IScenarioTokenAccessGuard
{
    /// <param name="analyseToken">Scenario isolation token; null selects the main plan</param>
    Task<bool> IsVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException (404) when the token selects a scenario the caller may not see or that does not exist.
    /// </summary>
    /// <param name="analyseToken">Scenario isolation token; null selects the main plan</param>
    Task EnsureVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default);
}
