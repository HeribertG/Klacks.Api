// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public interface IReplacementCandidatePoolResolver
{
    /// <summary>
    /// The candidate pool of a group, or null when the group does not exist. Neither capped nor narrowed to what a
    /// caller may see; both are up to the caller.
    /// </summary>
    /// <param name="groupId">Group that needs the stand-in; a root or a subgroup</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ReplacementCandidatePool?> ResolveAsync(Guid groupId, CancellationToken cancellationToken);
}
