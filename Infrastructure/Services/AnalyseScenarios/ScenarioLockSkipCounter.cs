// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Counts what an accept does not take over because it lies on a locked day: discarded scenario works that differ from
/// their real origin, and the work changes and expenses of discarded works that the real plan does not keep, matched by
/// content.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioLockSkipCounter
{
    private readonly DataBaseContext _context;

    public ScenarioLockSkipCounter(DataBaseContext context)
    {
        _context = context;
    }

    /// <summary>
    /// True when a discarded scenario work carries a change the real plan does not keep: it has no live real origin, or
    /// its employee, day, shift, times, hours or text differ from that origin. Its work changes and expenses are counted
    /// separately (CountDiscardedChildChangesAsync).
    /// </summary>
    internal static bool DiffersFromOrigin(Work clone, AcceptWorkSnapshot? origin, IReadOnlyDictionary<Guid, Guid> cloneToSource)
    {
        if (origin is null)
        {
            return true;
        }

        var shiftId = cloneToSource.TryGetValue(clone.ShiftId, out var sourceShiftId) ? sourceShiftId : clone.ShiftId;
        return clone.ClientId != origin.ClientId
            || clone.CurrentDate != origin.CurrentDate
            || shiftId != origin.ShiftId
            || clone.StartTime != origin.StartTime
            || clone.EndTime != origin.EndTime
            || clone.WorkTime != origin.WorkTime
            || !string.Equals(clone.Information, origin.Information, StringComparison.Ordinal);
    }

    /// <summary>
    /// Counts the work changes and expenses of discarded scenario works that the real plan does not keep, matched by
    /// content: per discarded work the larger of "scenario rows without an identical row on its real origin" and "origin
    /// rows without an identical scenario row", so an added, removed or edited row counts once. A discarded work without
    /// a real origin counts all its rows.
    /// </summary>
    /// <param name="discardedCloneIds">Scenario works of the period the accept discards</param>
    internal async Task<(int WorkChanges, int Expenses)> CountDiscardedChildChangesAsync(
        IReadOnlyCollection<Guid> discardedCloneIds, AcceptDayLockPlan plan, CancellationToken ct)
    {
        if (discardedCloneIds.Count == 0)
        {
            return (0, 0);
        }

        var workIds = discardedCloneIds
            .Concat(discardedCloneIds.Select(id => plan.RealOriginOf(id)?.Id).OfType<Guid>())
            .Distinct()
            .ToList();

        var workChanges = (await _context.WorkChange.IgnoreQueryFilters()
                .Where(wc => !wc.IsDeleted && workIds.Contains(wc.WorkId))
                .Select(wc => new
                {
                    wc.WorkId, wc.Type, wc.ChangeTime, wc.Surcharges, wc.StartTime, wc.EndTime,
                    wc.ReplaceClientId, wc.Description, wc.ToInvoice,
                })
                .ToListAsync(ct))
            .ToLookup(
                wc => wc.WorkId,
                wc => (wc.Type, wc.ChangeTime, wc.Surcharges, wc.StartTime, wc.EndTime, wc.ReplaceClientId, wc.Description, wc.ToInvoice));

        var expenses = (await _context.Expenses.IgnoreQueryFilters()
                .Where(e => !e.IsDeleted && workIds.Contains(e.WorkId))
                .Select(e => new { e.WorkId, e.Amount, e.Description, e.Taxable })
                .ToListAsync(ct))
            .ToLookup(e => e.WorkId, e => (e.Amount, e.Description, e.Taxable));

        return (
            CountChildDifferences(discardedCloneIds, plan, workChanges),
            CountChildDifferences(discardedCloneIds, plan, expenses));
    }

    private static int CountChildDifferences<TRow>(
        IReadOnlyCollection<Guid> discardedCloneIds, AcceptDayLockPlan plan, ILookup<Guid, TRow> rowsByWork)
        where TRow : notnull
    {
        var total = 0;
        foreach (var cloneId in discardedCloneIds)
        {
            var cloneRows = rowsByWork[cloneId].ToList();
            var originRows = plan.RealOriginOf(cloneId) is { } origin ? rowsByWork[origin.Id].ToList() : [];
            total += Math.Max(CountUnmatched(cloneRows, originRows), CountUnmatched(originRows, cloneRows));
        }

        return total;
    }

    private static int CountUnmatched<TRow>(IReadOnlyList<TRow> rows, IReadOnlyList<TRow> others)
        where TRow : notnull
    {
        var remaining = others.GroupBy(o => o).ToDictionary(g => g.Key, g => g.Count());
        var unmatched = 0;
        foreach (var row in rows)
        {
            if (remaining.TryGetValue(row, out var left) && left > 0)
            {
                remaining[row] = left - 1;
            }
            else
            {
                unmatched++;
            }
        }

        return unmatched;
    }
}
