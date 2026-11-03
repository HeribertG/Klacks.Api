// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IMoveReversalCheck
{
    /// <summary>
    /// Refuses taking a moved work back, with a MoveReversalRefusalCodes code, when it carries no origin (already back),
    /// something holds it in place (held), or the person it came from is not free at its time (origin busy). Shared by
    /// every way a move is taken back.
    /// </summary>
    /// <param name="work">The moved work as stored</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureReversibleAsync(Work work, CancellationToken cancellationToken);
}
