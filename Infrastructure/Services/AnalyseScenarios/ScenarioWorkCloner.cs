// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Clones the works of a scenario run: the top-level works of the window (limited to the group's shifts), their
/// sub-works, and the work changes and expenses of every cloned work. Each clone points at its cloned shift and parent
/// and keeps its origin in ClonedFromWorkId.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="groupScopeReader">Resolves the shifts of the group hierarchy in the world the clone copies from</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioWorkCloner
{
    private readonly DataBaseContext _context;
    private readonly ScenarioGroupScopeReader _groupScopeReader;

    public ScenarioWorkCloner(DataBaseContext context, ScenarioGroupScopeReader groupScopeReader)
    {
        _context = context;
        _groupScopeReader = groupScopeReader;
    }

    internal async Task<Dictionary<Guid, Guid>> CloneWorks(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, Guid token, Dictionary<Guid, Guid> shiftIdMap, ScenarioCloneSource source, CancellationToken ct)
    {
        var works = await LoadWorksToCloneAsync(groupIds, fromDate, untilDate, source, ct);

        var workIdMap = new Dictionary<Guid, Guid>();

        foreach (var work in works)
        {
            workIdMap[work.Id] = Guid.NewGuid();
        }

        foreach (var work in works)
        {
            await _context.Work.AddAsync(BuildClone(work, token, shiftIdMap, workIdMap), ct);
        }

        await CloneWorkChanges(workIdMap, token, source, ct);
        await CloneExpenses(workIdMap, token, source, ct);

        return workIdMap;
    }

    private async Task<List<Work>> LoadWorksToCloneAsync(List<Guid>? groupIds, DateOnly fromDate, DateOnly untilDate, ScenarioCloneSource source, CancellationToken ct)
    {
        IQueryable<Work> topLevelQuery = ScenarioSourceFilters.WhereFromSource(_context.Work
            .Where(w => !w.IsDeleted
                && w.ParentWorkId == null
                && w.CurrentDate >= fromDate
                && w.CurrentDate <= untilDate), source);

        if (groupIds != null)
        {
            var shiftIds = await _groupScopeReader.LoadGroupShiftIdsAsync(groupIds, source, ct);

            topLevelQuery = topLevelQuery.Where(w => shiftIds.Contains(w.ShiftId));
        }

        var topLevelWorks = await topLevelQuery.AsNoTracking().ToListAsync(ct);
        var topLevelIds = topLevelWorks.Select(w => w.Id).ToList();

        var subWorks = topLevelIds.Count == 0
            ? new List<Work>()
            : await ScenarioSourceFilters.WhereEitherWorld(_context.Work
                .Where(w => !w.IsDeleted
                    && w.ParentWorkId.HasValue
                    && topLevelIds.Contains(w.ParentWorkId.Value)), source)
                .AsNoTracking()
                .ToListAsync(ct);

        return topLevelWorks.Concat(subWorks).ToList();
    }

    private static Work BuildClone(Work work, Guid token, Dictionary<Guid, Guid> shiftIdMap, Dictionary<Guid, Guid> workIdMap)
        => new()
        {
            Id = workIdMap[work.Id],
            AnalyseToken = token,
            ClientId = work.ClientId,
            CurrentDate = work.CurrentDate,
            Information = work.Information,
            WorkTime = work.WorkTime,
            Surcharges = work.Surcharges,
            StartTime = work.StartTime,
            EndTime = work.EndTime,
            LockLevel = work.LockLevel,
            SealedAt = work.SealedAt,
            SealedBy = work.SealedBy,
            ShiftId = shiftIdMap.TryGetValue(work.ShiftId, out var newShiftId) ? newShiftId : work.ShiftId,
            ParentWorkId = work.ParentWorkId.HasValue && workIdMap.TryGetValue(work.ParentWorkId.Value, out var newParentId)
                ? newParentId
                : null,
            ClonedFromWorkId = work.Id,
            TransportMode = work.TransportMode
        };

    private async Task CloneWorkChanges(Dictionary<Guid, Guid> workIdMap, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var workChanges = await ScenarioSourceFilters.WhereEitherWorld(_context.WorkChange
            .Where(wc => !wc.IsDeleted && originalWorkIds.Contains(wc.WorkId)), source)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var wc in workChanges)
        {
            var clone = new WorkChange
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                WorkId = workIdMap[wc.WorkId],
                StartTime = wc.StartTime,
                EndTime = wc.EndTime,
                ChangeTime = wc.ChangeTime,
                Surcharges = wc.Surcharges,
                Type = wc.Type,
                Description = wc.Description,
                ToInvoice = wc.ToInvoice,
                ReplaceClientId = wc.ReplaceClientId
            };

            await _context.WorkChange.AddAsync(clone, ct);
        }
    }

    private async Task CloneExpenses(Dictionary<Guid, Guid> workIdMap, Guid token, ScenarioCloneSource source, CancellationToken ct)
    {
        if (workIdMap.Count == 0) return;

        var originalWorkIds = workIdMap.Keys.ToList();
        var expenses = await ScenarioSourceFilters.WhereEitherWorld(_context.Expenses
            .Where(e => !e.IsDeleted && originalWorkIds.Contains(e.WorkId)), source)
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var exp in expenses)
        {
            var clone = new Expenses
            {
                Id = Guid.NewGuid(),
                AnalyseToken = token,
                WorkId = workIdMap[exp.WorkId],
                Description = exp.Description,
                Amount = exp.Amount,
                Taxable = exp.Taxable
            };

            await _context.Expenses.AddAsync(clone, ct);
        }
    }
}
