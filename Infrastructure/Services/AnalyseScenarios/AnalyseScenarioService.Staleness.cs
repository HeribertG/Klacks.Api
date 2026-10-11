// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Staleness check of a scenario before it is accepted: counts the real rows inside the scope the accept replaces that
/// were created, updated or deleted after the scenario was created. The scope is resolved by the same helper the
/// accept delete uses (ResolveRealAcceptScopeAsync) and the row filters mirror SoftDeleteRealWorks/Breaks/
/// ScheduleNotes, so the check sees exactly what the accept would overwrite. Deleted rows are included
/// (IgnoreQueryFilters), because a deletion after the scenario was created is overwritten too: the accept brings the
/// scenario's copy of the row back.
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

    private const string MissingCreateTimeMessage =
        "Scenario {ScenarioId} has no creation time; the staleness check before accepting it is skipped";

    public async Task<ScenarioStaleness?> FindRealPlanChangesSinceCreationAsync(AnalyseScenario scenario, CancellationToken ct)
    {
        if (scenario.CreateTime is not { } since)
        {
            _logger.LogWarning(MissingCreateTimeMessage, scenario.Id);
            return null;
        }

        var fromDate = scenario.FromDate;
        var untilDate = scenario.UntilDate;
        var (shiftIds, clientIds) = await ResolveRealAcceptScopeAsync(scenario.GroupId, scenario.Token, fromDate, untilDate, ct);

        var scopedWorks = _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.None
                && w.CurrentDate >= fromDate && w.CurrentDate <= untilDate);
        if (shiftIds != null)
        {
            scopedWorks = scopedWorks.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var scopedWorkIds = scopedWorks.Select(w => w.Id);

        var scopedBreaks = _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == null && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate);
        var scopedNotes = _context.ScheduleNotes.IgnoreQueryFilters()
            .Where(sn => sn.AnalyseToken == null && sn.CurrentDate >= fromDate && sn.CurrentDate <= untilDate);
        if (clientIds != null)
        {
            scopedBreaks = scopedBreaks.Where(b => clientIds.Contains(b.ClientId));
            scopedNotes = scopedNotes.Where(sn => clientIds.Contains(sn.ClientId));
        }

        var changeTimes = new List<DateTime>();
        changeTimes.AddRange(await ChangeTimesSinceAsync(scopedWorks, since, ct));
        changeTimes.AddRange(await ChangeTimesSinceAsync(
            _context.WorkChange.IgnoreQueryFilters().Where(wc => wc.AnalyseToken == null && scopedWorkIds.Contains(wc.WorkId)),
            since,
            ct));
        changeTimes.AddRange(await ChangeTimesSinceAsync(
            _context.Expenses.IgnoreQueryFilters().Where(e => e.AnalyseToken == null && scopedWorkIds.Contains(e.WorkId)),
            since,
            ct));
        changeTimes.AddRange(await ChangeTimesSinceAsync(scopedBreaks, since, ct));
        changeTimes.AddRange(await ChangeTimesSinceAsync(scopedNotes, since, ct));

        if (changeTimes.Count == 0)
        {
            return null;
        }

        var acceptedScenarioNames = await AcceptedSiblingNamesSinceAsync(scenario, since, ct);

        return new ScenarioStaleness(changeTimes.Count, changeTimes.Max(), acceptedScenarioNames);
    }

    /// <summary>
    /// For every row of <paramref name="query"/> changed after <paramref name="since"/>, the latest of its creation,
    /// update and deletion instants. One entry per changed row.
    /// </summary>
    private static async Task<List<DateTime>> ChangeTimesSinceAsync<TEntity>(
        IQueryable<TEntity> query, DateTime since, CancellationToken ct)
        where TEntity : BaseEntity
    {
        var rows = await query
            .Where(ChangedSince<TEntity>(since))
            .Select(e => new { e.CreateTime, e.UpdateTime, e.DeletedTime })
            .ToListAsync(ct);

        return rows
            .Select(r => new[] { r.CreateTime, r.UpdateTime, r.DeletedTime }
                .Where(t => t.HasValue && t.Value > since)
                .Max()!.Value)
            .ToList();
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
