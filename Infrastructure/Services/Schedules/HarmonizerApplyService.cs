// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Materialises a cached harmonizer result into a new AnalyseScenario. The source schedule
/// stays untouched so the user can compare or roll back. For each non-Free cell in the best
/// bitmap a new Work is created that carries the harmonised agent assignment but reuses the
/// original Work's start time, end time, hours and shift reference (mapped through the
/// scenario clone's shift id map).
/// </summary>
/// <param name="resultCache">Cache populated by the harmonizer runner</param>
/// <param name="mediator">Mediator used to dispatch BulkAddWorksCommand</param>
/// <param name="scenarioRepository">Repository for persisting new AnalyseScenario entities</param>
/// <param name="scenarioService">Service for cloning schedule data into the new scenario</param>
/// <param name="unitOfWork">Unit of work for flushing EF changes</param>
/// <param name="context">EF Core database context for loading original Work metadata</param>
public sealed class HarmonizerApplyService : IHarmonizerApplyService
{
    private readonly HarmonizerResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;

    public HarmonizerApplyService(
        HarmonizerResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        DataBaseContext context)
    {
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct)
    {
        if (!_resultCache.TryGet(jobId, out _, out var bestBitmap, out _) || bestBitmap is null)
        {
            throw new InvalidOperationException($"No cached harmonizer result for job id {jobId}.");
        }

        var workIds = CollectWorkIds(bestBitmap);
        var originalWorks = await LoadWorksAsync(workIds, ct);

        var periodFrom = bestBitmap.Days.Count > 0 ? bestBitmap.Days[0] : DateOnly.FromDateTime(DateTime.UtcNow);
        var periodUntil = bestBitmap.Days.Count > 0 ? bestBitmap.Days[^1] : periodFrom;

        var name = await GenerateUniqueNameAsync(periodFrom, periodUntil, groupId, ct);
        var token = Guid.NewGuid();

        var analyseScenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = periodFrom,
            UntilDate = periodUntil,
            Token = token,
        };

        await _scenarioRepository.Add(analyseScenario);
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(groupId, periodFrom, periodUntil, token, ct);
        await _unitOfWork.CompleteAsync();

        // CloneScenarioDataAsync clones existing schedule entries (Works/Breaks/Expenses/WorkChanges)
        // into the new scenario. The harmonizer replaces those with rearranged Works derived from the
        // best bitmap, so we must soft-delete the cloned schedule entries first — otherwise both the
        // cloned originals and the harmonised replacements would coexist in the new scenario, leading
        // to duplicate work entries on every day after the user accepts the scenario.
        await DeleteClonedScheduleEntriesAsync(token, periodFrom, periodUntil, ct);
        await _unitOfWork.CompleteAsync();

        var bulkItems = BuildBulkItems(bestBitmap, originalWorks, shiftIdMap, token);

        IReadOnlyList<Guid> createdIds = [];
        if (bulkItems.Count > 0)
        {
            var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
            {
                Works = bulkItems,
                PeriodStart = periodFrom,
                PeriodEnd = periodUntil,
            }));
            createdIds = response.CreatedIds;
        }

        _resultCache.Invalidate(jobId);

        var resource = new AnalyseScenarioResource
        {
            Id = analyseScenario.Id,
            Name = analyseScenario.Name,
            Description = analyseScenario.Description,
            GroupId = analyseScenario.GroupId,
            FromDate = analyseScenario.FromDate,
            UntilDate = analyseScenario.UntilDate,
            Token = analyseScenario.Token,
            CreatedByUser = analyseScenario.CreatedByUser,
            Status = (int)analyseScenario.Status,
        };

        return (resource, createdIds);
    }

    private static List<Guid> CollectWorkIds(HarmonyBitmap bitmap)
    {
        var ids = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                foreach (var id in cell.WorkIds)
                {
                    ids.Add(id);
                }
            }
        }
        return ids.ToList();
    }

    private async Task<Dictionary<Guid, Work>> LoadWorksAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(ct);
        return works.ToDictionary(w => w.Id);
    }

    private async Task DeleteClonedScheduleEntriesAsync(Guid token, DateOnly from, DateOnly until, CancellationToken ct)
    {
        var clonedWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.CurrentDate >= from && w.CurrentDate <= until && !w.IsDeleted)
            .ToListAsync(ct);
        var workIds = clonedWorks.Select(w => w.Id).ToList();

        var now = DateTime.UtcNow;

        var clonedBreaks = await _context.Break.IgnoreQueryFilters()
            .Where(b => b.AnalyseToken == token && b.CurrentDate >= from && b.CurrentDate <= until && !b.IsDeleted)
            .ToListAsync(ct);
        foreach (var b in clonedBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

        if (workIds.Count > 0)
        {
            var orphanSubBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && workIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in orphanSubBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

            var clonedWorkChanges = await _context.WorkChange
                .Where(wc => !wc.IsDeleted && workIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in clonedWorkChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

            var clonedExpenses = await _context.Expenses
                .Where(e => !e.IsDeleted && workIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in clonedExpenses) { e.IsDeleted = true; e.DeletedTime = now; }
        }

        foreach (var w in clonedWorks) { w.IsDeleted = true; w.DeletedTime = now; }
    }

    private static List<BulkWorkItem> BuildBulkItems(
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Work> originalWorks,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid analyseToken)
    {
        var items = new List<BulkWorkItem>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var newAgentId = Guid.Parse(bitmap.Rows[r].Id);
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.Symbol == CellSymbol.Free || cell.WorkIds.Count == 0)
                {
                    continue;
                }
                foreach (var workId in cell.WorkIds)
                {
                    if (!originalWorks.TryGetValue(workId, out var original))
                    {
                        continue;
                    }
                    var shiftId = shiftIdMap.TryGetValue(original.ShiftId, out var mapped) ? mapped : original.ShiftId;
                    items.Add(new BulkWorkItem
                    {
                        ClientId = newAgentId,
                        ShiftId = shiftId,
                        CurrentDate = bitmap.Days[d],
                        StartTime = original.StartTime,
                        EndTime = original.EndTime,
                        WorkTime = original.WorkTime,
                        Information = null,
                        AnalyseToken = analyseToken,
                    });
                }
            }
        }
        return items;
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken ct)
    {
        var baseName = $"Harmonisiert {from:dd.MM.yy} – {until:dd.MM.yy}";
        var existing = await _scenarioRepository.GetByGroupAsync(groupId, ct);
        var existingNames = existing.Select(s => s.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var counter = 2;
        while (true)
        {
            var candidate = $"{baseName} ({counter})";
            if (!existingNames.Contains(candidate))
            {
                return candidate;
            }
            counter++;
        }
    }
}
