// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Tells whether a user currently has proposal-scenario activity they may see: an AutoWizard run in progress
/// or an open (Active) analyse scenario, both inside the user's group scope.
/// </summary>
public interface IScenarioActivityProbe
{
    Task<bool> HasVisibleActivityAsync(Guid userId, CancellationToken cancellationToken = default);
}
