// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICoverMoveFactsReader
{
    /// <summary>
    /// The facts of the given works of one plan, keyed by work id. A work outside that plan is left out.
    /// </summary>
    /// <param name="workIds">The works</param>
    /// <param name="analyseToken">Token of the scenario; null is the real plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyDictionary<Guid, CoverMoveFacts>> LoadAsync(
        IReadOnlyCollection<Guid> workIds, Guid? analyseToken, CancellationToken cancellationToken);

    /// <summary>
    /// Whether this person holds no work and no blocking absence in the window, as the scenario sees them: its own rows plus
    /// the real works it has no clone of. An on-call absence does not block.
    /// </summary>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="clientId">The person</param>
    /// <param name="startAt">Start of the window</param>
    /// <param name="endAt">End of the window</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> IsFreeAsync(Guid analyseToken, Guid clientId, DateTime startAt, DateTime endAt, CancellationToken cancellationToken);
}
