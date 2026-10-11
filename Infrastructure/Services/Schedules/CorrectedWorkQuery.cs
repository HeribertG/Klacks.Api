// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads corrected works (works carrying a correction, travel or briefing WorkChange, see CorrectionWorkChangeTypes) for
/// the planning wizards. Shared by the wizard context builders, which lock a corrected work like a sealed one and count
/// its correction hours, and by the apply paths, which must never move or delete it - the recorded time belongs to the
/// work's own employee. LoadFixedWorkIdsAsync is the single protection set of the apply paths: replacement covers plus
/// corrected works.
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
    /// <returns>Corrected work id -> hours its corrections add to the work (as the period hours count them)</returns>
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

        return rows
            .GroupBy(r => r.WorkId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(r => CorrectionWorkChangeTypes.IsHourBearing(r.Type)).Sum(r => r.ChangeTime));
    }

    public static async Task<HashSet<Guid>> LoadFixedWorkIdsAsync(
        DataBaseContext context, Guid? analyseToken, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var fixedIds = await ReplacementCoverQuery.LoadCoveredWorkIdsAsync(context, analyseToken, fromDate, untilDate, ct);
        fixedIds.UnionWith((await LoadAsync(context, analyseToken, fromDate, untilDate, null, ct)).Keys);
        return fixedIds;
    }
}
