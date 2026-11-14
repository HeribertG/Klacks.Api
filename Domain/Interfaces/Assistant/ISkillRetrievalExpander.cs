// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRetrievalExpander
{
    /// <summary>
    /// How many of the last retrieval places graph neighbours compete for; 0 switches the association off.
    /// </summary>
    int AssociationSlots { get; }

    /// <summary>
    /// Ranks the graph neighbours of the seed skills that the association pool scored and returns at most slots names,
    /// best first. Empty when the turn's exploration skip fires or no neighbour is in the pool.
    /// </summary>
    /// <param name="agentId">Agent whose skill relations form the graph</param>
    /// <param name="seedScores">The retrieved skills that keep their places, with their cosine scores</param>
    /// <param name="poolScores">The eligible pool candidates (permitted, not always-on, not seeds) with their cosine scores</param>
    /// <param name="slots">The number of places to fill at most</param>
    /// <param name="cancellationToken">Cancellation of the turn</param>
    Task<IReadOnlyList<string>> AssociateAsync(
        Guid agentId,
        IReadOnlyDictionary<string, double> seedScores,
        IReadOnlyDictionary<string, double> poolScores,
        int slots,
        CancellationToken cancellationToken = default);
}
