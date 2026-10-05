// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Display names ("Name FirstName") of the clients a set of works concerns: the work owners and the clients who
/// take over part of a work through a replacement work change. The first name found per client wins.
/// </summary>
/// <param name="works">Works with their Client loaded</param>
/// <param name="workChanges">Work changes with their ReplaceClient loaded</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ScheduleClientNames
{
    public static Dictionary<Guid, string> Build(IEnumerable<Work> works, IEnumerable<WorkChange> workChanges)
    {
        var lookup = new Dictionary<Guid, string>();
        foreach (var work in works)
        {
            if (work.Client != null)
            {
                lookup.TryAdd(work.ClientId, $"{work.Client.Name} {work.Client.FirstName}".Trim());
            }
        }

        foreach (var change in workChanges)
        {
            if (change.ReplaceClient != null && change.ReplaceClientId.HasValue)
            {
                lookup.TryAdd(change.ReplaceClientId.Value, $"{change.ReplaceClient.Name} {change.ReplaceClient.FirstName}".Trim());
            }
        }

        return lookup;
    }
}
