// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Walks works along ClonedFromWorkId to the real plan, up to ScenarioSourceChainLimits.MaxDepth steps. A work without a real
/// origin within the limit has none in the result.
/// </summary>

using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal static class WorkCloneLineage
{
    /// <param name="context">Database context of the caller</param>
    /// <param name="works">Work id and its clone source</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Work id to the real-plan work at the end of its clone chain</returns>
    internal static async Task<IReadOnlyDictionary<Guid, WorkRealOrigin>> LoadRealOriginsAsync(
        DataBaseContext context, IEnumerable<(Guid WorkId, Guid? ClonedFromWorkId)> works, CancellationToken ct)
    {
        var origins = new Dictionary<Guid, WorkRealOrigin>();
        var pending = works
            .Where(work => work.ClonedFromWorkId is not null)
            .GroupBy(work => work.ClonedFromWorkId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(work => work.WorkId).ToList());

        for (var depth = 0; pending.Count > 0 && depth < ScenarioSourceChainLimits.MaxDepth; depth++)
        {
            var ids = pending.Keys.ToList();
            var links = await context.Work.IgnoreQueryFilters().AsNoTracking()
                .Where(w => ids.Contains(w.Id))
                .Select(w => new { w.Id, w.ClonedFromWorkId, w.AnalyseToken, w.MovedFromClientId })
                .ToListAsync(ct);

            var next = new Dictionary<Guid, List<Guid>>();
            foreach (var link in links)
            {
                if (link.AnalyseToken is null)
                {
                    foreach (var workId in pending[link.Id])
                    {
                        origins[workId] = new WorkRealOrigin(link.Id, link.MovedFromClientId);
                    }
                }
                else if (link.ClonedFromWorkId is { } source)
                {
                    if (!next.TryGetValue(source, out var starts))
                    {
                        next[source] = starts = [];
                    }

                    starts.AddRange(pending[link.Id]);
                }
            }

            pending = next;
        }

        return origins;
    }
}
