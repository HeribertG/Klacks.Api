// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Staleness check of a scenario before it is accepted: counts the real rows inside the scope the accept replaces that
/// were created, updated or deleted after the scenario's snapshot was taken. The scope is resolved by the same helper the
/// accept delete uses (ResolveRealAcceptScopeAsync). Real works count whatever their lock level: a work sealed after the
/// snapshot stays in place on accept while its movable clone is promoted next to it. Work changes and expenses count
/// only on movable works, because the accept never overwrites the children of a locked work. Deleted rows are included
/// (IgnoreQueryFilters), because a deletion after the snapshot is overwritten too: the accept brings the scenario's copy
/// of the row back. A scenario built on another scenario carries rows of its source's snapshot, so the snapshot instant
/// is the earliest creation along the SourceToken chain. Counting and the latest change instant are aggregated in the
/// database.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    private const int MaxReportedAcceptedScenarioNames = 5;

    private const int MaxSourceChainDepth = 16;

    private const string MissingCreateTimeMessage =
        "Scenario {ScenarioId} has no creation time; the staleness check before accepting it is skipped";

    private const string SourceChainTooDeepMessage =
        "Scenario {ScenarioId}: the SourceToken chain is deeper than {MaxDepth} scenarios; the staleness check uses the earliest creation found so far";

    private const string SourceChainCycleMessage =
        "Scenario {ScenarioId}: the SourceToken chain contains a cycle at token {Token}; the staleness check uses the earliest creation found so far";

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
        var (shiftIds, clientIds) = await ResolveRealAcceptScopeAsync(scenario.GroupId, scenario.Token, fromDate, untilDate, ct);

        var scopedWorks = _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == null && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);
        if (shiftIds != null)
        {
            scopedWorks = scopedWorks.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var movableWorkIds = scopedWorks.Where(w => w.LockLevel == WorkLockLevel.None).Select(w => w.Id);

        var scopedBreaks = _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == null && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);
        var scopedNotes = _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == null && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);
        if (clientIds != null)
        {
            scopedBreaks = scopedBreaks.Where(b => clientIds.Contains(b.ClientId));
            scopedNotes = scopedNotes.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var summaries = new[]
        {
            await SummarizeChangesSinceAsync(scopedWorks, since, ct),
            await SummarizeChangesSinceAsync(
                _context.WorkChange.IgnoreQueryFilters().Where(wc => wc.AnalyseToken == null && movableWorkIds.Contains(wc.WorkId)),
                since,
                ct),
            await SummarizeChangesSinceAsync(
                _context.Expenses.IgnoreQueryFilters().Where(e => e.AnalyseToken == null && movableWorkIds.Contains(e.WorkId)),
                since,
                ct),
            await SummarizeChangesSinceAsync(scopedBreaks, since, ct),
            await SummarizeChangesSinceAsync(scopedNotes, since, ct),
        };

        var changedEntryCount = summaries.Sum(s => s.Count);
        if (changedEntryCount == 0)
        {
            return null;
        }

        var lastChange = summaries.Where(s => s.LastChange.HasValue).Max(s => s.LastChange!.Value);
        var acceptedScenarioNames = await AcceptedSiblingNamesSinceAsync(scenario, since, ct);

        return new ScenarioStaleness(changedEntryCount, lastChange, acceptedScenarioNames);
    }

    /// <summary>
    /// The instant of the snapshot the scenario's rows stand for: its own creation, or the earlier creation of a source
    /// scenario it was built on (transitively). Deleted or decided ancestors still count. A cycle or a chain deeper than
    /// <see cref="MaxSourceChainDepth"/> stops the walk with a warning.
    /// </summary>
    private async Task<DateTime> EarliestCreationAlongSourceChainAsync(AnalyseScenario scenario, CancellationToken ct)
    {
        var earliest = scenario.CreateTime!.Value;
        var visited = new HashSet<Guid> { scenario.Token };
        var next = scenario.SourceToken;

        for (var depth = 0; next is { } sourceToken; depth++)
        {
            if (depth == MaxSourceChainDepth)
            {
                _logger.LogWarning(SourceChainTooDeepMessage, scenario.Id, MaxSourceChainDepth);
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

    /// <summary>
    /// Number of rows of <paramref name="query"/> changed after <paramref name="since"/> and the latest of their creation,
    /// update and deletion instants. Every counted row has at least one instant after <paramref name="since"/>, so the
    /// largest column maximum equals the latest per-row change.
    /// </summary>
    private static async Task<(int Count, DateTime? LastChange)> SummarizeChangesSinceAsync<TEntity>(
        IQueryable<TEntity> query, DateTime since, CancellationToken ct)
        where TEntity : BaseEntity
    {
        var changed = query.Where(ChangedSince<TEntity>(since));
        var count = await changed.CountAsync(ct);
        if (count == 0)
        {
            return (0, null);
        }

        var lastCreated = await changed.MaxAsync(e => e.CreateTime, ct);
        var lastUpdated = await changed.MaxAsync(e => e.UpdateTime, ct);
        var lastDeleted = await changed.MaxAsync(e => e.DeletedTime, ct);
        var lastChange = new[] { lastCreated, lastUpdated, lastDeleted }.Where(t => t.HasValue).Max();

        return (count, lastChange);
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
