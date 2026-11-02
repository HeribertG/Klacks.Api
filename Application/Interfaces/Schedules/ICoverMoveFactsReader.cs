// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICoverMoveFactsReader
{
    /// <summary>
    /// The facts of the given works of one scenario, keyed by work id. A work outside the scenario is left out.
    /// </summary>
    /// <param name="workIds">Scenario works</param>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyDictionary<Guid, CoverMoveFacts>> LoadAsync(
        IReadOnlyCollection<Guid> workIds, Guid analyseToken, CancellationToken cancellationToken);
}
