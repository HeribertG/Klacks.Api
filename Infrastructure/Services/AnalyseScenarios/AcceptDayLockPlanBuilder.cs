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
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="logger">Logs clone lineages deeper than the source-chain limit</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class AcceptDayLockPlanBuilder
{
    private const string CloneLineageTooDeepMessage =
        "Accepting scenario {Token}: the clone lineage of {Count} works is deeper than {MaxDepth}; they are treated as having no real origin";

    private readonly DataBaseContext _context;
    private readonly ILogger<AcceptDayLockPlanBuilder> _logger;

    public AcceptDayLockPlanBuilder(DataBaseContext context, ILogger<AcceptDayLockPlanBuilder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Builds the day-lock plan of an accept from the database state.
    /// </summary>
    /// <param name="token">Scenario token</param>
    /// <param name="fromDate">First day of the accept period</param>
    /// <param name="untilDate">Last day of the accept period</param>
    /// <param name="realScopeClientIds">Clients of the real rows the delete looks at, so their days are resolved too</param>
    /// <param name="realScopeWorkIds">Real works the delete looks at, so the days of their work changes are resolved too</param>
    internal async Task<AcceptDayLockPlan> BuildAcceptDayLockPlanAsync(
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
            .Select(AcceptRowSnapshots.Work)
            .ToListAsync(ct);

        var originByClone = await ResolveRealOriginsAsync(token, clones, ct);
        var subBreaks = await LoadSubBreaksOfAsync(token, clones, originByClone.Values, ct);
        var workChangeCells = await LoadWorkChangeCellsOfAsync(
            clones.Select(c => c.Id).Concat(originByClone.Values.Select(o => o.Id)).Concat(realScopeWorkIds), ct);

        var (scenarioBreakClients, scenarioNoteClients) = await LoadScenarioDayRowClientsAsync(token, fromDate, untilDate, ct);

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

    private async Task<(List<Guid> BreakClients, List<Guid> NoteClients)> LoadScenarioDayRowClientsAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
    {
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
        return (scenarioBreakClients, scenarioNoteClients);
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

        for (var depth = 0; pending.Count > 0 && depth < ScenarioSourceChainLimits.MaxDepth; depth++)
        {
            var ancestorIds = pending.Values.Distinct().ToList();
            var ancestors = await _context.Work.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(w => ancestorIds.Contains(w.Id))
                .Select(AcceptRowSnapshots.Work)
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
            _logger.LogWarning(CloneLineageTooDeepMessage, token, pending.Count, ScenarioSourceChainLimits.MaxDepth);
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
                .Select(AcceptRowSnapshots.Break)
                .ToListAsync(ct));
        }

        if (originIds.Count > 0)
        {
            result.AddRange(await _context.Break.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(b => b.AnalyseToken == null && !b.IsDeleted
                    && b.ParentWorkId.HasValue && originIds.Contains(b.ParentWorkId.Value))
                .Select(AcceptRowSnapshots.Break)
                .ToListAsync(ct));
        }

        return result;
    }
}
