// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads replacement covers (works handed to a substitute by a replacement WorkChange) for the planning wizards.
/// Shared by the wizard context builders, which lock the covered work and mark the substitute as occupied, and by
/// the apply paths, which must never move or delete a covered work. Keyed on the WorkChange type, not on the
/// recovery's free-text marker, so manual replacements entered by a planner are protected the same way.
/// </summary>
/// <param name="context">Database context of the caller (soft-delete filters apply)</param>
/// <param name="analyseToken">Scenario token; null reads the real plan</param>
/// <param name="fromDate">First date (inclusive)</param>
/// <param name="untilDate">Last date (inclusive)</param>
/// <param name="agentIds">When set, only covers whose original or substitute is one of these agents</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ReplacementCoverQuery
{
    public static async Task<List<ReplacementCover>> LoadAsync(
        DataBaseContext context,
        Guid? analyseToken,
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? agentIds,
        CancellationToken ct)
    {
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();

        var joined =
            from change in context.WorkChange.AsNoTracking()
            join work in context.Work.AsNoTracking() on change.WorkId equals work.Id
            where replacementTypes.Contains(change.Type)
                  && change.ReplaceClientId != null
                  && work.CurrentDate >= fromDate
                  && work.CurrentDate <= untilDate
                  && (work.AnalyseToken == analyseToken || (work.AnalyseToken == null && analyseToken == null))
                  && (change.AnalyseToken == analyseToken || (change.AnalyseToken == null && analyseToken == null))
            select new { change, work };

        if (agentIds is not null)
        {
            var agents = agentIds.ToList();
            joined = joined.Where(x => agents.Contains(x.work.ClientId) || agents.Contains(x.change.ReplaceClientId!.Value));
        }

        var rows = await joined
            .Select(x => new
            {
                WorkId = x.work.Id,
                x.work.ClientId,
                SubstituteId = x.change.ReplaceClientId!.Value,
                x.work.ShiftId,
                x.work.CurrentDate,
                WorkStart = x.work.StartTime,
                WorkEnd = x.work.EndTime,
                x.change.Type,
                ChangeStart = x.change.StartTime,
                ChangeEnd = x.change.EndTime,
                x.change.ChangeTime,
            })
            .ToListAsync(ct);

        var covers = new List<ReplacementCover>(rows.Count);
        foreach (var r in rows)
        {
            var hours = ReplacementWindow.ClampHours(r.WorkStart, r.WorkEnd, r.ChangeTime);
            var (start, end) = ReplacementWindow.Compute(r.Type, r.WorkStart, r.WorkEnd, r.ChangeStart, r.ChangeEnd, hours);
            if (hours <= 0m || start == end)
            {
                continue;
            }

            var (startAt, endAt) = ReplacementWindow.ToInterval(r.CurrentDate, r.WorkStart, start, end);
            covers.Add(new ReplacementCover(
                r.WorkId, r.ClientId, r.SubstituteId, r.ShiftId, r.CurrentDate, start, end, startAt, endAt, hours));
        }

        return covers;
    }

    public static async Task<HashSet<Guid>> LoadCoveredWorkIdsAsync(
        DataBaseContext context, Guid? analyseToken, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
        var covers = await LoadAsync(context, analyseToken, fromDate, untilDate, null, ct);
        return covers.Select(c => c.WorkId).ToHashSet();
    }
}
