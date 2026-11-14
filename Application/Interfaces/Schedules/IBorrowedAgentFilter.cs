// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Takes the people who are only borrowed into the planning group out of the agents of a planning run (see
/// BorrowedAgentFilter).
/// </summary>
public interface IBorrowedAgentFilter
{
    /// <summary>
    /// Removes the agents that are only borrowed into the planning group from the agents of a planning run, keeping the given order.
    /// </summary>
    /// <param name="agentIds">Agents of the run in their planning order</param>
    /// <param name="groupId">Group the run plans for; null = none</param>
    /// <param name="analyseToken">Source scenario of the run; its group stands in when the run names none</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agents without the borrowed ones, in the given order</returns>
    Task<IReadOnlyList<Guid>> RemoveBorrowedAsync(
        IReadOnlyList<Guid> agentIds, Guid? groupId, Guid? analyseToken, CancellationToken cancellationToken);
}
