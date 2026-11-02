// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads corrected works (works carrying a correction, travel or briefing WorkChange, see CorrectionWorkChangeTypes) and
/// moved works (Work.MovedFromClientId set by a cover, with 0 hours of their own) for the planning wizards. Shared by the
/// wizard context builders, which lock such a work like a sealed one and count its correction hours, and by the apply paths,
/// which must never move or delete it - the recorded time belongs to the work's own employee, and a moved work is settled.
/// LoadFixedWorkIdsAsync is the single protection set of the apply paths: replacement covers plus corrected and moved works.
/// </summary>
/// <param name="context">Database context of the caller (soft-delete filters apply)</param>
/// <param name="analyseToken">Scenario token; null reads the real plan</param>
/// <param name="fromDate">First date (inclusive)</param>
/// <param name="untilDate">Last date (inclusive)</param>
/// <param name="agentIds">When set, only works of these agents</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class CorrectedWorkQuery
{
    /// <returns>Corrected or moved work id -> hours its corrections add to the work (as the period hours count them)</returns>
    public static async Task<Dictionary<Guid, decimal>> LoadAsync(
        DataBaseContext context,
        Guid? analyseToken,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? agentIds,
        CancellationToken ct)
    {
        var correctionTypes = CorrectionWorkChangeTypes.All.ToList();

        var joined =
            from change in context.WorkChange.AsNoTracking()
            join work in context.Work.AsNoTracking() on change.WorkId equals work.Id
            where correctionTypes.Contains(change.Type)
                  && work.CurrentDate >= fromDate
                  && work.CurrentDate <= untilDate
                  && (work.AnalyseToken == analyseToken || (work.AnalyseToken == null && analyseToken == null))
                  && (change.AnalyseToken == analyseToken || (change.AnalyseToken == null && analyseToken == null))
            select new { change, work };

        if (agentIds is not null)
        {
            var agents = agentIds.ToList();
            joined = joined.Where(x => agents.Contains(x.work.ClientId));
        }

        var rows = await joined
            .Select(x => new { WorkId = x.work.Id, x.change.Type, x.change.ChangeTime })
            .ToListAsync(ct);

        var fixedWorks = rows
            .GroupBy(r => r.WorkId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(r => CorrectionWorkChangeTypes.IsHourBearing(r.Type)).Sum(r => r.ChangeTime));

        foreach (var movedId in await LoadMovedWorkIdsAsync(context, analyseToken, fromDate, untilDate, agentIds, ct))
        {
            fixedWorks.TryAdd(movedId, 0m);
        }

        return fixedWorks;
    }

    private static async Task<List<Guid>> LoadMovedWorkIdsAsync(
        DataBaseContext context,
        Guid? analyseToken,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? agentIds,
        CancellationToken ct)
    {
        var moved = context.Work.AsNoTracking()
            .Where(work => work.MovedFromClientId != null
                && work.CurrentDate >= fromDate
                && work.CurrentDate <= untilDate
                && (work.AnalyseToken == analyseToken || (work.AnalyseToken == null && analyseToken == null)));

        if (agentIds is not null)
        {
            var agents = agentIds.ToList();
            moved = moved.Where(work => agents.Contains(work.ClientId));
        }

        return await moved.Select(work => work.Id).ToListAsync(ct);
    }

    public static async Task<HashSet<Guid>> LoadFixedWorkIdsAsync(
        DataBaseContext context, Guid? analyseToken, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var fixedIds = await ReplacementCoverQuery.LoadCoveredWorkIdsAsync(context, analyseToken, fromDate, untilDate, ct);
        fixedIds.UnionWith((await LoadAsync(context, analyseToken, fromDate, untilDate, null, ct)).Keys);
        return fixedIds;
    }
}
