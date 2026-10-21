// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Staleness check of a scenario before it is accepted: counts the real rows inside the scope the accept replaces that
/// were created, updated or deleted after the scenario's snapshot was taken AND that the accept actually overwrites. The
/// scope is resolved by the same helper the accept delete uses (ScenarioAcceptScopeResolver), and the rows the accept
/// leaves in place are dropped with the same day-lock plan the accept builds (AcceptDayLockPlan): rows on a locked day,
/// locked works and breaks, a work with a work change shown on a locked day, kept slots, and the children and
/// pauses of every kept work. Such a change survives the accept, so the warning must not call it overwritten. Deleted
/// rows are included (IgnoreQueryFilters), because a deletion after the snapshot is overwritten too: the accept brings
/// the scenario's copy of the row back. A scenario built on another scenario carries rows of its source's snapshot, so
/// the snapshot instant is the earliest creation along the SourceToken chain. The changed rows are filtered in memory,
/// because the locked (client, day) pairs do not translate into SQL.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="scopeResolver">Resolves the real shifts and clients the accept replaces</param>
/// <param name="dayLockPlanBuilder">Builds the day-lock plan of the accept</param>
/// <param name="logger">Logs a missing creation time and a cut-short source chain</param>

using System.Linq.Expressions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioStalenessDetector
{
    private const int MaxReportedAcceptedScenarioNames = 5;

    private const string MissingCreateTimeMessage =
        "Scenario {ScenarioId} has no creation time; the staleness check before accepting it is skipped";

    private const string SourceChainTooDeepMessage =
        "Scenario {ScenarioId}: the SourceToken chain is deeper than {MaxDepth} scenarios; the staleness check uses the earliest creation found so far";

    private const string SourceChainCycleMessage =
        "Scenario {ScenarioId}: the SourceToken chain contains a cycle at token {Token}; the staleness check uses the earliest creation found so far";

    private readonly DataBaseContext _context;
    private readonly ScenarioAcceptScopeResolver _scopeResolver;
    private readonly AcceptDayLockPlanBuilder _dayLockPlanBuilder;
    private readonly ILogger<ScenarioStalenessDetector> _logger;

    public ScenarioStalenessDetector(
        DataBaseContext context,
        ScenarioAcceptScopeResolver scopeResolver,
        AcceptDayLockPlanBuilder dayLockPlanBuilder,
        ILogger<ScenarioStalenessDetector> logger)
    {
        _context = context;
        _scopeResolver = scopeResolver;
        _dayLockPlanBuilder = dayLockPlanBuilder;
        _logger = logger;
    }

    public async Task<ScenarioStaleness?> FindRealPlanChangesSinceCreationAsync(AnalyseScenario scenario, CancellationToken ct)
    {
        if (scenario.CreateTime is null)
        {
            _logger.LogWarning(MissingCreateTimeMessage, scenario.Id);
            return null;
        }

        var since = await EarliestCreationAlongSourceChainAsync(scenario, ct);
        var fromDate = scenario.FromDate;
        var untilDate = scenario.UntilDate;
        var (shiftIds, clientIds) = await _scopeResolver.ResolveRealAcceptScopeAsync(scenario.GroupId, scenario.Token, fromDate, untilDate, ct);

        var changed = await LoadChangedRowsAsync(since, fromDate, untilDate, shiftIds, clientIds, ct);

        if (changed.Count == 0)
        {
            return null;
        }

        var overwritten = await KeepOverwrittenRowsAsync(scenario.Token, fromDate, untilDate, shiftIds, changed, ct);
        if (overwritten.Count == 0)
        {
            return null;
        }

        var lastChange = overwritten
            .SelectMany(r => new[] { r.CreateTime, r.UpdateTime, r.DeletedTime })
            .Where(t => t > since)
            .Max()!.Value;
        var acceptedScenarioNames = await AcceptedSiblingNamesSinceAsync(scenario, since, ct);

        return new ScenarioStaleness(overwritten.Count, lastChange, acceptedScenarioNames);
    }

    /// <summary>
    /// The real rows of the accept scope created, updated or deleted after the snapshot: works, the work changes and
    /// expenses of movable works, breaks and schedule notes.
    /// </summary>
    private async Task<List<StaleRow>> LoadChangedRowsAsync(
        DateTime since, DateOnly fromDate, DateOnly untilDate, List<Guid>? shiftIds, List<Guid>? clientIds, CancellationToken ct)
    {
        var scopedWorks = _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == null && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);
        if (shiftIds != null)
        {
            scopedWorks = scopedWorks.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var movableWorks = scopedWorks.Where(w => w.LockLevel == WorkLockLevel.None);

        var scopedBreaks = _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == null && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);
        var scopedNotes = _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == null && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);
        if (clientIds != null)
        {
            scopedBreaks = scopedBreaks.Where(b => clientIds.Contains(b.ClientId));
            scopedNotes = scopedNotes.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var changedSince = ChangedSince<Work>(since);
        var changed = new List<StaleRow>();
        changed.AddRange(await scopedWorks.Where(changedSince)
            .Select(w => new StaleRow(StaleRowKind.Work, w.Id, w.ClientId, w.CurrentDate, w.LockLevel, w.Id, w.CreateTime, w.UpdateTime, w.DeletedTime))
            .ToListAsync(ct));
        changed.AddRange(await (
                from wc in _context.WorkChange.IgnoreQueryFilters()
                join w in movableWorks on wc.WorkId equals w.Id
                where wc.AnalyseToken == null && (wc.CreateTime > since || wc.UpdateTime > since || wc.DeletedTime > since)
                select new StaleRow(StaleRowKind.WorkChild, wc.Id, w.ClientId, w.CurrentDate, w.LockLevel, w.Id, wc.CreateTime, wc.UpdateTime, wc.DeletedTime))
            .ToListAsync(ct));
        changed.AddRange(await (
                from e in _context.Expenses.IgnoreQueryFilters()
                join w in movableWorks on e.WorkId equals w.Id
                where e.AnalyseToken == null && (e.CreateTime > since || e.UpdateTime > since || e.DeletedTime > since)
                select new StaleRow(StaleRowKind.WorkChild, e.Id, w.ClientId, w.CurrentDate, w.LockLevel, w.Id, e.CreateTime, e.UpdateTime, e.DeletedTime))
            .ToListAsync(ct));
        changed.AddRange(await scopedBreaks.Where(ChangedSince<Break>(since))
            .Select(b => new StaleRow(StaleRowKind.Break, b.Id, b.ClientId, b.CurrentDate, b.LockLevel, b.ParentWorkId, b.CreateTime, b.UpdateTime, b.DeletedTime))
            .ToListAsync(ct));
        changed.AddRange(await scopedNotes.Where(ChangedSince<ScheduleNote>(since))
            .Select(sn => new StaleRow(StaleRowKind.Note, sn.Id, sn.ClientId, sn.CurrentDate, WorkLockLevel.None, null, sn.CreateTime, sn.UpdateTime, sn.DeletedTime))
            .ToListAsync(ct));

        return changed;
    }

    /// <summary>
    /// Drops the changed rows the accept leaves in place, decided by the same day-lock plan the accept builds: rows on a
    /// locked day, locked works and breaks, and the children and pauses of a real work the accept keeps (locked, on a
    /// locked day, with a work change shown on a locked day, or kept as a slot). Those changes are not overwritten,
    /// so they must not be reported as such. A pause survives exactly when the delete keeps it: its paused work is a live
    /// real work of the scope that the accept leaves in place, or a work whose slot the plan keeps.
    /// </summary>
    /// <param name="shiftIds">Real shifts of the accept scope, or null for every shift</param>
    private async Task<List<StaleRow>> KeepOverwrittenRowsAsync(
        Guid token, DateOnly fromDate, DateOnly untilDate, List<Guid>? shiftIds, List<StaleRow> changed, CancellationToken ct)
    {
        var pauseParentIds = changed
            .Where(r => r.Kind == StaleRowKind.Break && r.WorkId.HasValue)
            .Select(r => r.WorkId!.Value)
            .Distinct()
            .ToList();
        var pauseParents = pauseParentIds.Count == 0
            ? new Dictionary<Guid, AcceptWorkSnapshot>()
            : await _context.Work.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(w => pauseParentIds.Contains(w.Id) && w.AnalyseToken == null)
                .Select(AcceptRowSnapshots.Work)
                .ToDictionaryAsync(w => w.Id, ct);

        var workIds = changed.Where(r => r.Kind != StaleRowKind.Note && r.Kind != StaleRowKind.Break)
            .Select(r => r.WorkId!.Value)
            .Concat(pauseParents.Keys)
            .Distinct()
            .ToList();
        var clientIds = changed.Select(r => r.ClientId).Concat(pauseParents.Values.Select(p => p.ClientId)).Distinct().ToList();
        var plan = await _dayLockPlanBuilder.BuildAcceptDayLockPlanAsync(token, fromDate, untilDate, clientIds, workIds, ct);

        var keptWorkIds = plan.KeptRealWorkIds
            .Concat(pauseParents.Values
                .Where(parent => !parent.IsDeleted
                    && parent.CurrentDate >= fromDate && parent.CurrentDate <= untilDate
                    && (shiftIds == null || shiftIds.Contains(parent.ShiftId))
                    && plan.LeavesRealWork(parent.Id, parent.ClientId, parent.CurrentDate, parent.LockLevel))
                .Select(parent => parent.Id))
            .ToHashSet();

        bool Overwritten(StaleRow row)
        {
            switch (row.Kind)
            {
                case StaleRowKind.Work:
                case StaleRowKind.WorkChild:
                    return !plan.LeavesRealWork(row.WorkId!.Value, row.ClientId, row.CurrentDate, row.LockLevel);
                case StaleRowKind.Break:
                    return !plan.LeavesRealBreak(row.LockLevel, row.ClientId, row.CurrentDate, row.WorkId, keptWorkIds);
                default:
                    return !plan.IsLocked(row.ClientId, row.CurrentDate);
            }
        }

        return changed.Where(Overwritten).ToList();
    }

    /// <summary>
    /// The instant of the snapshot the scenario's rows stand for: its own creation, or the earlier creation of a source
    /// scenario it was built on (transitively). Deleted or decided ancestors still count. A cycle or a chain deeper than
    /// <see cref="ScenarioSourceChainLimits.MaxDepth"/> stops the walk with a warning.
    /// </summary>
    private async Task<DateTime> EarliestCreationAlongSourceChainAsync(AnalyseScenario scenario, CancellationToken ct)
    {
        var earliest = scenario.CreateTime!.Value;
        var visited = new HashSet<Guid> { scenario.Token };
        var next = scenario.SourceToken;

        for (var depth = 0; next is { } sourceToken; depth++)
        {
            if (depth == ScenarioSourceChainLimits.MaxDepth)
            {
                _logger.LogWarning(SourceChainTooDeepMessage, scenario.Id, ScenarioSourceChainLimits.MaxDepth);
                break;
            }

            if (!visited.Add(sourceToken))
            {
                _logger.LogWarning(SourceChainCycleMessage, scenario.Id, sourceToken);
                break;
            }

            var source = await _context.AnalyseScenarios.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.Token == sourceToken)
                .Select(s => new { s.CreateTime, s.SourceToken })
                .FirstOrDefaultAsync(ct);
            if (source is null)
            {
                break;
            }

            if (source.CreateTime is { } created && created < earliest)
            {
                earliest = created;
            }

            next = source.SourceToken;
        }

        return earliest;
    }

    private static Expression<Func<TEntity, bool>> ChangedSince<TEntity>(DateTime since)
        where TEntity : BaseEntity
        => e => e.CreateTime > since || e.UpdateTime > since || e.DeletedTime > since;

    private async Task<IReadOnlyList<string>> AcceptedSiblingNamesSinceAsync(AnalyseScenario scenario, DateTime since, CancellationToken ct)
    {
        var scenarioId = scenario.Id;
        var groupId = scenario.GroupId;
        var fromDate = scenario.FromDate;
        var untilDate = scenario.UntilDate;

        return await _context.AnalyseScenarios
            .Where(s => s.Id != scenarioId
                && s.Status == AnalyseScenarioStatus.Accepted
                && s.UpdateTime > since
                && s.GroupId == groupId
                && s.FromDate <= untilDate && s.UntilDate >= fromDate)
            .OrderByDescending(s => s.UpdateTime)
            .Select(s => s.Name)
            .Take(MaxReportedAcceptedScenarioNames)
            .ToListAsync(ct);
    }
}
