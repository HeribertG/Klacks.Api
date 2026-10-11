// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Correlates the works of a scenario-sourced harmonizer bitmap with the new scenario's clone. Inside the source
/// scenario's range the clone copies exactly the bitmap's works (direct map). A boundary copy outside that range (the
/// clone took the real plan there) follows its Work.ClonedFromWorkId chain to the real work the clone copied, and is
/// correlated only when that clone still sits on the same root shift and date; a real work moved since the source
/// scenario was made is superseded: the real plan's state wins and its clone stays untouched. A legacy copy without the
/// link falls back to a key match (employee, root shift, date, start and end time) that must be unique on both sides.
/// A clone is never claimed twice.
/// </summary>
/// <param name="context">Database context of the apply (reads works, soft-delete filters ignored)</param>
/// <param name="scenarioService">Resolves scenario shifts to their real root shift</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ScenarioSourceWorkCorrelator
{
    /// <summary>
    /// Upper bound of ClonedFromWorkId hops (scenario of a scenario of ...); guards against a cycle in corrupt data.
    /// </summary>
    private const int MaxCloneOriginDepth = 16;

    private readonly DataBaseContext _context;
    private readonly IAnalyseScenarioService _scenarioService;

    public ScenarioSourceWorkCorrelator(DataBaseContext context, IAnalyseScenarioService scenarioService)
    {
        _context = context;
        _scenarioService = scenarioService;
    }

    /// <param name="bitmapWorkIds">Every work id the bitmap references</param>
    /// <param name="bitmapWorks">The bitmap's works as loaded before the apply (their pre-harmonizing assignment)</param>
    /// <param name="workIdMap">Source work id -> clone work id, as returned by the scenario clone</param>
    public async Task<ScenarioSourceWorkCorrelation> CorrelateAsync(
        IReadOnlyCollection<Guid> bitmapWorkIds,
        IReadOnlyDictionary<Guid, Work> bitmapWorks,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken ct)
    {
        var pending = bitmapWorkIds.Where(id => !workIdMap.ContainsKey(id)).ToList();
        if (pending.Count == 0)
        {
            return ScenarioSourceWorkCorrelation.Direct(workIdMap);
        }

        var claimedClones = bitmapWorkIds.Where(workIdMap.ContainsKey).Select(id => workIdMap[id]).ToHashSet();
        var correlated = new Dictionary<Guid, Guid>(workIdMap);
        var supersededBitmapWorkIds = new HashSet<Guid>();
        var supersededCloneIds = new HashSet<Guid>();

        var chainTargets = await FollowCloneOriginsAsync(pending, workIdMap, ct);
        var cloneIds = workIdMap.Values.Distinct().ToList();
        var clones = await _context.Work.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => cloneIds.Contains(w.Id) && !w.IsDeleted)
            .ToDictionaryAsync(w => w.Id, ct);
        var rootOf = await LoadRootShiftResolverAsync(
            clones.Values.Select(w => w.ShiftId).Concat(bitmapWorks.Values.Select(w => w.ShiftId)), ct);

        foreach (var (bitmapWorkId, cloneId) in chainTargets)
        {
            if (!clones.TryGetValue(cloneId, out var clone) || !bitmapWorks.TryGetValue(bitmapWorkId, out var bitmapWork))
            {
                continue;
            }

            if (rootOf(clone.ShiftId) != rootOf(bitmapWork.ShiftId) || clone.CurrentDate != bitmapWork.CurrentDate)
            {
                supersededBitmapWorkIds.Add(bitmapWorkId);
                supersededCloneIds.Add(cloneId);
                claimedClones.Add(cloneId);
                continue;
            }

            if (claimedClones.Add(cloneId))
            {
                correlated[bitmapWorkId] = cloneId;
            }
        }

        var legacy = pending
            .Where(id => !correlated.ContainsKey(id) && !supersededBitmapWorkIds.Contains(id) && !chainTargets.ContainsKey(id))
            .Where(bitmapWorks.ContainsKey)
            .ToList();
        foreach (var (bitmapWorkId, cloneId) in MatchByUniqueKey(legacy, bitmapWorks, clones, claimedClones, rootOf))
        {
            correlated[bitmapWorkId] = cloneId;
        }

        return new ScenarioSourceWorkCorrelation(correlated, supersededBitmapWorkIds, supersededCloneIds);
    }

    /// <summary>
    /// Follows each pending work's ClonedFromWorkId chain until it reaches a work the clone copied.
    /// </summary>
    /// <returns>Pending work id -> clone of the first chain member the clone copied</returns>
    private async Task<Dictionary<Guid, Guid>> FollowCloneOriginsAsync(
        IReadOnlyCollection<Guid> pending, IReadOnlyDictionary<Guid, Guid> workIdMap, CancellationToken ct)
    {
        var targets = new Dictionary<Guid, Guid>();
        var cursor = pending.ToDictionary(id => id, id => id);

        for (var depth = 0; depth < MaxCloneOriginDepth && cursor.Count > 0; depth++)
        {
            var currentIds = cursor.Values.Distinct().ToList();
            var origins = await _context.Work.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(w => currentIds.Contains(w.Id) && w.ClonedFromWorkId != null)
                .ToDictionaryAsync(w => w.Id, w => w.ClonedFromWorkId!.Value, ct);

            var next = new Dictionary<Guid, Guid>();
            foreach (var (pendingId, currentId) in cursor)
            {
                if (!origins.TryGetValue(currentId, out var originId))
                {
                    continue;
                }

                if (workIdMap.TryGetValue(originId, out var cloneId))
                {
                    targets[pendingId] = cloneId;
                    continue;
                }

                next[pendingId] = originId;
            }

            cursor = next;
        }

        return targets;
    }

    private static IEnumerable<(Guid BitmapWorkId, Guid CloneId)> MatchByUniqueKey(
        IReadOnlyCollection<Guid> legacyBitmapWorkIds,
        IReadOnlyDictionary<Guid, Work> bitmapWorks,
        IReadOnlyDictionary<Guid, Work> clones,
        ISet<Guid> claimedClones,
        Func<Guid, Guid> rootOf)
    {
        if (legacyBitmapWorkIds.Count == 0)
        {
            yield break;
        }

        var candidatesByKey = clones.Values
            .Where(c => !claimedClones.Contains(c.Id) && c.ParentWorkId == null)
            .GroupBy(c => KeyOf(c, rootOf))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var group in legacyBitmapWorkIds.GroupBy(id => KeyOf(bitmapWorks[id], rootOf)))
        {
            if (group.Count() != 1
                || !candidatesByKey.TryGetValue(group.Key, out var candidates)
                || candidates.Count != 1
                || !claimedClones.Add(candidates[0].Id))
            {
                continue;
            }

            yield return (group.Single(), candidates[0].Id);
        }
    }

    private static (Guid ClientId, Guid RootShiftId, DateOnly Date, TimeOnly Start, TimeOnly End) KeyOf(
        Work work, Func<Guid, Guid> rootOf)
        => (work.ClientId, rootOf(work.ShiftId), work.CurrentDate, work.StartTime, work.EndTime);

    private async Task<Func<Guid, Guid>> LoadRootShiftResolverAsync(IEnumerable<Guid> shiftIds, CancellationToken ct)
    {
        var roots = await _scenarioService.ResolveRootShiftIdsAsync(shiftIds.Distinct().ToList(), ct);
        return shiftId => roots.TryGetValue(shiftId, out var root) ? root : shiftId;
    }
}
