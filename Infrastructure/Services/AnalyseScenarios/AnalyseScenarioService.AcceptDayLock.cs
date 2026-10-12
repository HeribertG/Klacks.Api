// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Day lock of the accept: locked days (sealed days, attributed exactly like the day lock of single edits, see
/// DayLockAttribution) are left untouched by an accept. Real works, breaks and schedule notes on a locked day survive,
/// and the scenario's works, breaks and notes on a locked day are discarded instead of promoted. Works move as a slot:
/// when the real work or any of its clones (or a pause of either) is locked, the real work stays and all its clones are
/// discarded, including the other members of a container family. A work counts as locked too, on either side, when one
/// of its work changes shows on a locked day: on the day the schedule shows it (WorkChangeEntryDay, the following day
/// for the post-midnight part of a shift crossing midnight), for the work's employee or a replacement employee. The
/// locked days are therefore resolved up to the day after the period. Breaks that pause a work follow the work. The plan is
/// read from the database before anything is staged; the delete and the promote each build it and get the same answer,
/// because staged changes are not visible to these queries until the accept commits.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    private const string LockSkipsMessage =
        "Accepting scenario {Token}: {Works} works, {WorkChanges} work changes, {Expenses} expenses, {Breaks} breaks and {Notes} schedule notes on locked days were not taken over";

    private const string CloneLineageTooDeepMessage =
        "Accepting scenario {Token}: the clone lineage of {Count} works is deeper than {MaxDepth}; they are treated as having no real origin";

    private static readonly Expression<Func<Work, AcceptWorkSnapshot>> WorkSnapshot = w => new AcceptWorkSnapshot(
        w.Id, w.ClientId, w.CurrentDate, w.ShiftId, w.StartTime, w.EndTime, w.WorkTime, w.Information,
        w.LockLevel, w.ParentWorkId, w.ClonedFromWorkId, w.AnalyseToken, w.IsDeleted);

    private static readonly Expression<Func<Break, AcceptBreakSnapshot>> BreakSnapshot = b => new AcceptBreakSnapshot(
        b.Id, b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime, b.LockLevel, b.ParentWorkId);

    /// <summary>
    /// Builds the day-lock plan of an accept from the database state.
    /// </summary>
    /// <param name="token">Scenario token</param>
    /// <param name="fromDate">First day of the accept period</param>
    /// <param name="untilDate">Last day of the accept period</param>
    /// <param name="realScopeClientIds">Clients of the real rows the delete looks at, so their days are resolved too</param>
    /// <param name="realScopeWorkIds">Real works the delete looks at, so the days of their work changes are resolved too</param>
    private async Task<AcceptDayLockPlan> BuildAcceptDayLockPlanAsync(
        Guid token,
        DateOnly fromDate,
        DateOnly untilDate,
        IEnumerable<Guid> realScopeClientIds,
        IEnumerable<Guid> realScopeWorkIds,
        CancellationToken ct)
    {
        var clones = await _context.Work.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate)
            .Select(WorkSnapshot)
            .ToListAsync(ct);

        var originByClone = await ResolveRealOriginsAsync(token, clones, ct);
        var subBreaks = await LoadSubBreaksOfAsync(token, clones, originByClone.Values, ct);
        var workChangeCells = await LoadWorkChangeCellsOfAsync(
            clones.Select(c => c.Id).Concat(originByClone.Values.Select(o => o.Id)).Concat(realScopeWorkIds), ct);

        var scenarioBreakClients = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate)
            .Select(b => b.ClientId)
            .Distinct()
            .ToListAsync(ct);
        var scenarioNoteClients = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate)
            .Select(sn => sn.ClientId)
            .Distinct()
            .ToListAsync(ct);

        var clientIds = clones.Select(c => c.ClientId)
            .Concat(originByClone.Values.Select(o => o.ClientId))
            .Concat(subBreaks.Select(b => b.ClientId))
            .Concat(scenarioBreakClients)
            .Concat(scenarioNoteClients)
            .Concat(realScopeClientIds)
            .Concat(workChangeCells.Select(c => c.ClientId))
            .Distinct()
            .ToList();

        var lockedDays = await DayLockAttribution.LoadLockedClientDaysAsync(
            _context, clientIds, fromDate, untilDate.AddDays(1), ct);

        var lockedPauseParents = subBreaks
            .Where(b => b.LockLevel != WorkLockLevel.None || lockedDays.Contains((b.ClientId, b.CurrentDate)))
            .Select(b => b.ParentWorkId!.Value)
            .ToHashSet();
        var workChangeLocked = workChangeCells
            .Where(c => lockedDays.Contains((c.ClientId, c.Date)))
            .Select(c => c.WorkId)
            .ToHashSet();

        bool IsSideLocked(AcceptWorkSnapshot work)
            => work.LockLevel != WorkLockLevel.None
                || lockedDays.Contains((work.ClientId, work.CurrentDate))
                || lockedPauseParents.Contains(work.Id)
                || workChangeLocked.Contains(work.Id);

        var discarded = clones
            .Where(c => IsSideLocked(c) || (originByClone.TryGetValue(c.Id, out var origin) && IsSideLocked(origin)))
            .Select(c => c.Id)
            .ToHashSet();
        var kept = CloseLockedSlots(clones, originByClone, discarded);

        return new AcceptDayLockPlan(lockedDays, kept, discarded, originByClone, workChangeLocked);
    }

    /// <summary>
    /// The (employee, day) cells the live work changes of the given works (real or scenario) show on: the day comes
    /// from WorkChangeEntryDay (the following day for the post-midnight part of a shift crossing midnight), the
    /// employees are the work's own and, for a replacement, the replacement employee.
    /// </summary>
    private async Task<List<(Guid WorkId, Guid ClientId, DateOnly Date)>> LoadWorkChangeCellsOfAsync(
        IEnumerable<Guid> workIds, CancellationToken ct)
    {
        var ids = workIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (
                from wc in _context.WorkChange.IgnoreQueryFilters()
                join w in _context.Work.IgnoreQueryFilters() on wc.WorkId equals w.Id
                join s in _context.Shift.IgnoreQueryFilters() on w.ShiftId equals s.Id
                where !wc.IsDeleted && ids.Contains(wc.WorkId)
                select new
                {
                    wc.WorkId, w.ClientId, wc.ReplaceClientId, wc.Type, w.CurrentDate, w.EndTime,
                    wc.StartTime, wc.ChangeTime, s.StartShift, s.EndShift,
                })
            .ToListAsync(ct);

        var cells = new List<(Guid WorkId, Guid ClientId, DateOnly Date)>();
        foreach (var row in rows)
        {
            var day = WorkChangeEntryDay.Of(
                row.Type, row.CurrentDate, row.EndTime, row.StartTime, row.ChangeTime, row.StartShift, row.EndShift);
            cells.Add((row.WorkId, row.ClientId, day));
            if (row.ReplaceClientId is { } replaceClientId)
            {
                cells.Add((row.WorkId, replaceClientId, day));
            }
        }

        return cells.Distinct().ToList();
    }

    /// <summary>
    /// Extends the discarded clones to whole slots and container families until nothing changes: the real origin of a
    /// discarded clone is kept, every other clone of a kept origin is discarded, and a discarded member discards its
    /// whole container family (parent work and sub-works). Returns the kept real works.
    /// </summary>
    private static HashSet<Guid> CloseLockedSlots(
        IReadOnlyCollection<AcceptWorkSnapshot> clones,
        IReadOnlyDictionary<Guid, AcceptWorkSnapshot> originByClone,
        HashSet<Guid> discarded)
    {
        var kept = new HashSet<Guid>();
        bool changed;
        do
        {
            changed = false;
            foreach (var clone in clones.Where(c => discarded.Contains(c.Id)))
            {
                if (originByClone.TryGetValue(clone.Id, out var origin))
                {
                    changed |= kept.Add(origin.Id);
                }
            }

            var discardedFamilies = clones.Where(c => discarded.Contains(c.Id)).Select(FamilyKeyOf).ToHashSet();
            foreach (var clone in clones.Where(c => !discarded.Contains(c.Id)))
            {
                var keptOrigin = originByClone.TryGetValue(clone.Id, out var origin) && kept.Contains(origin.Id);
                if (keptOrigin || discardedFamilies.Contains(FamilyKeyOf(clone)))
                {
                    changed |= discarded.Add(clone.Id);
                }
            }
        }
        while (changed);

        return kept;
    }

    private static Guid FamilyKeyOf(AcceptWorkSnapshot work) => work.ParentWorkId ?? work.Id;

    /// <summary>
    /// The live real work each scenario clone stands for, following ClonedFromWorkId through source scenarios (a clone of
    /// a clone) until a real work is reached. Clones without lineage, with a deleted real origin or with a lineage deeper
    /// than the source-chain limit have no entry.
    /// </summary>
    private async Task<Dictionary<Guid, AcceptWorkSnapshot>> ResolveRealOriginsAsync(
        Guid token, IReadOnlyCollection<AcceptWorkSnapshot> clones, CancellationToken ct)
    {
        var originByClone = new Dictionary<Guid, AcceptWorkSnapshot>();
        var pending = clones
            .Where(c => c.ClonedFromWorkId.HasValue)
            .ToDictionary(c => c.Id, c => c.ClonedFromWorkId!.Value);

        for (var depth = 0; pending.Count > 0 && depth < MaxSourceChainDepth; depth++)
        {
            var ancestorIds = pending.Values.Distinct().ToList();
            var ancestors = await _context.Work.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(w => ancestorIds.Contains(w.Id))
                .Select(WorkSnapshot)
                .ToDictionaryAsync(w => w.Id, ct);

            var next = new Dictionary<Guid, Guid>();
            foreach (var (cloneId, ancestorId) in pending)
            {
                if (!ancestors.TryGetValue(ancestorId, out var ancestor))
                {
                    continue;
                }

                if (ancestor.AnalyseToken is null)
                {
                    if (!ancestor.IsDeleted)
                    {
                        originByClone[cloneId] = ancestor;
                    }
                }
                else if (ancestor.ClonedFromWorkId is { } upstreamId)
                {
                    next[cloneId] = upstreamId;
                }
            }

            pending = next;
        }

        if (pending.Count > 0)
        {
            _logger.LogWarning(CloneLineageTooDeepMessage, token, pending.Count, MaxSourceChainDepth);
        }

        return originByClone;
    }

    private async Task<List<AcceptBreakSnapshot>> LoadSubBreaksOfAsync(
        Guid token,
        IReadOnlyCollection<AcceptWorkSnapshot> clones,
        IEnumerable<AcceptWorkSnapshot> origins,
        CancellationToken ct)
    {
        var cloneIds = clones.Select(c => c.Id).ToList();
        var originIds = origins.Select(o => o.Id).Distinct().ToList();
        var result = new List<AcceptBreakSnapshot>();

        if (cloneIds.Count > 0)
        {
            result.AddRange(await _context.Break.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.AnalyseToken == token && !b.IsDeleted
                    && b.ParentWorkId.HasValue && cloneIds.Contains(b.ParentWorkId.Value))
                .Select(BreakSnapshot)
                .ToListAsync(ct));
        }

        if (originIds.Count > 0)
        {
            result.AddRange(await _context.Break.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.AnalyseToken == null && !b.IsDeleted
                    && b.ParentWorkId.HasValue && originIds.Contains(b.ParentWorkId.Value))
                .Select(BreakSnapshot)
                .ToListAsync(ct));
        }

        return result;
    }

    /// <summary>
    /// True when a discarded scenario work carries a change the real plan does not keep: it has no live real origin, or
    /// its employee, day, shift, times, hours or text differ from that origin. Its work changes and expenses are counted
    /// separately (CountDiscardedChildChangesAsync).
    /// </summary>
    private static bool DiffersFromOrigin(Work clone, AcceptWorkSnapshot? origin, IReadOnlyDictionary<Guid, Guid> cloneToSource)
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
    private async Task<(int WorkChanges, int Expenses)> CountDiscardedChildChangesAsync(
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

    /// <summary>
    /// Promotes the scenario breaks of the period, except those on a locked day, those pausing a discarded scenario work,
    /// and copies of an absence the real plan keeps as a locked break (the real one survives the delete, so promoting
    /// the copy would duplicate it). Boundary breaks are discarded as before. Returns how many discarded breaks have no
    /// identical real break kept on their day.
    /// </summary>
    private async Task<int> PromoteScenarioBreaksAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, DateTime now, CancellationToken ct)
    {
        var scenarioBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted).ToListAsync(ct);

        var clientIds = scenarioBreaks.Select(b => b.ClientId).Distinct().ToList();
        var realBreaks = clientIds.Count == 0
            ? new List<AcceptBreakSnapshot>()
            : await _context.Break.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.AnalyseToken == null && !b.IsDeleted
                    && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate
                    && clientIds.Contains(b.ClientId))
                .Select(BreakSnapshot)
                .ToListAsync(ct);

        var lockedRealAbsences = realBreaks
            .Where(b => b.LockLevel != WorkLockLevel.None)
            .Select(b => (b.ClientId, b.CurrentDate, b.AbsenceId))
            .ToHashSet();
        var keptParentIds = realBreaks
            .Where(b => b.ParentWorkId is { } parentId && (plan.KeepsRealWork(parentId) || plan.IsWorkChangeLocked(parentId)))
            .Select(b => b.ParentWorkId!.Value)
            .ToHashSet();
        var keptRealBreaks = realBreaks
            .Where(b => plan.LeavesRealBreak(b.LockLevel, b.ClientId, b.CurrentDate, b.ParentWorkId, keptParentIds))
            .Select(b => (b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime))
            .ToHashSet();

        var skipped = 0;
        foreach (var b in scenarioBreaks)
        {
            if (b.CurrentDate < fromDate || b.CurrentDate > untilDate)
            {
                b.IsDeleted = true;
                b.DeletedTime = now;
            }
            else if (plan.IsLocked(b.ClientId, b.CurrentDate)
                || (b.ParentWorkId is { } parentId && plan.DiscardsClone(parentId))
                || lockedRealAbsences.Contains((b.ClientId, b.CurrentDate, b.AbsenceId)))
            {
                b.IsDeleted = true;
                b.DeletedTime = now;
                if (!keptRealBreaks.Contains((b.ClientId, b.CurrentDate, b.AbsenceId, b.StartTime, b.EndTime)))
                {
                    skipped++;
                }
            }
            else
            {
                b.AnalyseToken = null;
            }
        }

        return skipped;
    }

    /// <summary>
    /// Promotes the scenario schedule notes of the period, except those on a locked day (the real notes there survive the
    /// delete). Boundary notes are discarded as before. Returns how many discarded notes have no identical real note on
    /// their day.
    /// </summary>
    private async Task<int> PromoteScenarioNotesAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, AcceptDayLockPlan plan, DateTime now, CancellationToken ct)
    {
        var scenarioNotes = await _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == token && !sn.IsDeleted).ToListAsync(ct);

        var lockedClientIds = scenarioNotes
            .Where(sn => sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate && plan.IsLocked(sn.ClientId, sn.CurrentDate))
            .Select(sn => sn.ClientId)
            .Distinct()
            .ToList();
        var keptRealNotes = lockedClientIds.Count == 0
            ? new HashSet<(Guid ClientId, DateOnly CurrentDate, string Content)>()
            : (await _context.ScheduleNotes.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(sn => sn.AnalyseToken == null && !sn.IsDeleted
                    && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate
                    && lockedClientIds.Contains(sn.ClientId))
                .Select(sn => new { sn.ClientId, sn.CurrentDate, sn.Content })
                .ToListAsync(ct))
                .Select(sn => (sn.ClientId, sn.CurrentDate, sn.Content))
                .ToHashSet();

        var skipped = 0;
        foreach (var sn in scenarioNotes)
        {
            if (sn.CurrentDate < fromDate || sn.CurrentDate > untilDate)
            {
                sn.IsDeleted = true;
                sn.DeletedTime = now;
            }
            else if (plan.IsLocked(sn.ClientId, sn.CurrentDate))
            {
                sn.IsDeleted = true;
                sn.DeletedTime = now;
                if (!keptRealNotes.Contains((sn.ClientId, sn.CurrentDate, sn.Content)))
                {
                    skipped++;
                }
            }
            else
            {
                sn.AnalyseToken = null;
            }
        }

        return skipped;
    }

    private void LogLockSkips(Guid token, ScenarioAcceptLockSkips skips)
    {
        if (skips.Total > 0)
        {
            _logger.LogInformation(
                LockSkipsMessage, token, skips.Works, skips.WorkChanges, skips.Expenses, skips.Breaks, skips.Notes);
        }
    }
}
