// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Interfaces;

public interface IErpDropPointRepository : IBaseRepository<ErpDropPoint>
{
    /// <summary>
    /// Records the moment the import runner looked into the drop point folder. Self-committing and untracked
    /// (ExecuteUpdate): it neither flushes nor is rolled back by pending or discarded changes of the calling scope,
    /// and it leaves the audit columns alone because a poll is runtime state, not a user edit.
    /// </summary>
    /// <param name="dropPointId">Drop point that was polled</param>
    /// <param name="polledAtUtc">UTC moment of the poll</param>
    /// <param name="cancellationToken">Cancels the update</param>
    /// <returns>Number of rows updated: 1, or 0 when the drop point no longer exists</returns>
    Task<int> MarkPolledAsync(Guid dropPointId, DateTime polledAtUtc, CancellationToken cancellationToken = default);
}
