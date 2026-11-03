// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IMovedWorkReversal
{
    /// <summary>
    /// Stages the reversal when the work goes back to the person it was moved from: the origin of the work is cleared and an
    /// applied Move row of that step gets ReversedAtUtc. Any other target changes nothing. Call it before the hand-over, while
    /// the work still names its current owner; the caller commits.
    /// </summary>
    /// <param name="work">The tracked work</param>
    /// <param name="targetClientId">The person the work goes to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StageAsync(Work work, Guid targetClientId, CancellationToken cancellationToken);
}
