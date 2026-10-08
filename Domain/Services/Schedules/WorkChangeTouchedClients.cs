// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The clients a WorkChange touches, which every read and write of a change must check against the caller's
/// group visibility: the owner of its parent Work and, for a replacement, the client moved in. One place so the
/// get, list, create, update and delete paths can never disagree about whose visibility a change depends on.
/// </summary>
/// <param name="parentWork">The Work the change hangs off; null when it is missing</param>
/// <param name="replaceClientId">The replacement client of the change, if any</param>
/// <param name="clientIds">Further client ids that are touched (e.g. old owner and old replacement on a move)</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkChangeTouchedClients
{
    public static IReadOnlyList<Guid> Of(Work? parentWork, Guid? replaceClientId)
        => Collect(parentWork?.ClientId, replaceClientId);

    public static IReadOnlyList<Guid> Collect(params Guid?[] clientIds)
        => clientIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
}
