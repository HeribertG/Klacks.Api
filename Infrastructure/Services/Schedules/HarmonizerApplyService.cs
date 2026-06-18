// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
public class HarmonizerApplyService : IHarmonizerApplyService
{
    /// <summary>
    /// Prefix used for scenario names. Subclasses (e.g. <c>HolisticHarmonizerApplyService</c>) override
    /// this to distinguish their output in the scenario list.
    /// </summary>
    protected virtual string ScenarioNamePrefix => "Harmonisiert";

    private readonly HarmonizerResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;
    private readonly ILogger _logger;

    public HarmonizerApplyService(
        HarmonizerResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        ILogger<HarmonizerApplyService> logger)
    {
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct,
        string? namePrefixOverride = null)
    {
        if (!_resultCache.TryGet(jobId, out var originalBitmap, out var bestBitmap, out var sourceAnalyseToken) || bestBitmap is null)
        {
            throw new InvalidOperationException($"No cached harmonizer result for job id {jobId}.");
        }

        var runGroupId = await ResolveRunGroupIdAsync(sourceAnalyseToken, ct);

        var workIds = CollectWorkIds(bestBitmap);
        var originalWorks = await LoadWorksAsync(workIds, ct);

        var periodFrom = bestBitmap.Days.Count > 0 ? bestBitmap.Days[0] : DateOnly.FromDateTime(DateTime.UtcNow);
        var periodUntil = bestBitmap.Days.Count > 0 ? bestBitmap.Days[^1] : periodFrom;

        var bitmapShiftIds = CollectBitmapShiftIds(bestBitmap, originalWorks);

        _logger.LogInformation(
            "HarmonizerApply jobId={JobId} bitmap rows={Rows} days={Days} periodFrom={From} periodUntil={Until} workIds={WorkIds} originalWorks={OriginalWorks} bitmapShifts={BitmapShifts}",
            jobId, bestBitmap.RowCount, bestBitmap.DayCount, periodFrom, periodUntil, workIds.Count, originalWorks.Count, bitmapShiftIds.Count);

        var name = await GenerateUniqueNameAsync(periodFrom, periodUntil, groupId, ct, namePrefixOverride);
        var token = Guid.NewGuid();

        var analyseScenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = periodFrom,
            UntilDate = periodUntil,
            Token = token,
            RunGroupId = runGroupId,
        };

        await _scenarioRepository.Add(analyseScenario);
        var (shiftIdMap, workIdMap) = await _scenarioService.CloneScenarioDataWithMapsAsync(groupId, periodFrom, periodUntil, token, bitmapShiftIds, ct);
        await _unitOfWork.CompleteAsync();

        // CloneScenarioDataAsync clones the existing schedule (Works + their WorkChange/Expense/sub-Break
        // children) into the new scenario. Materialise the harmonised result:
        //  - Real-plan source (W4 + standalone-on-real): RE-POINT the movable cloned works in place
        //    (set ClientId/date from the bitmap) so their children survive — the bitmap's real work ids
        //    map to the clones via workIdMap (C4 fix).
        //  - Scenario source: keep the original delete+recreate (the bitmap references scenario-token
        //    works the real-data clone does not cover, so re-pointing cannot correlate them).
        // Locked works and absence breaks are left intact either way.
        IReadOnlyList<Guid> createdIds;
        if (sourceAnalyseToken is null)
        {
            createdIds = await RepointClonedWorksAsync(token, periodFrom, periodUntil, bestBitmap, workIdMap, ct);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation(
                "HarmonizerApply jobId={JobId} re-pointed {Count} cloned works in place (children preserved).",
                jobId, createdIds.Count);
        }
        else
        {
            await DeleteClonedScheduleEntriesAsync(token, periodFrom, periodUntil, ct);
            await _unitOfWork.CompleteAsync();

            var bulkItems = BuildBulkItems(bestBitmap, originalWorks, shiftIdMap, token);
            _logger.LogInformation(
                "HarmonizerApply jobId={JobId} (scenario source) bulkItems={Total}",
                jobId, bulkItems.Count);

            createdIds = [];
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
            RunGroupId = analyseScenario.RunGroupId,
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

    /// <summary>
    /// Collects every original Shift ID referenced by the bitmap so the scenario
    /// clone covers them even when they have no GroupItem in the resolved group
    /// hierarchy. The harmonizer can carry shifts (e.g. container/sporadic) that
    /// the GroupItem-based clone filter misses; without these IDs the resulting
    /// shiftIdMap would be incomplete and BuildBulkItems would skip those works,
    /// leaving the scenario schedule empty after Apply.
    /// </summary>
    private static HashSet<Guid> CollectBitmapShiftIds(HarmonyBitmap bitmap, IReadOnlyDictionary<Guid, Work> originalWorks)
    {
        var ids = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.ShiftRefId is { } shiftRefId)
                {
                    ids.Add(shiftRefId);
                }
            }
        }
        foreach (var work in originalWorks.Values)
        {
            ids.Add(work.ShiftId);
        }
        return ids;
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
        // Only movable (non-locked) cloned works are replaced by the harmonised result.
        // Locked works (Confirmed/Approved/Closed) and absence Breaks are immutable — the
        // harmonizer never moves them, so they must survive into the new scenario untouched.
        // Deleting them here (and rebuilding works without their LockLevel) would silently
        // unlock sealed works and drop vacation/sick breaks once the scenario is accepted.
        var movableWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.CurrentDate >= from && w.CurrentDate <= until
                && !w.IsDeleted && w.LockLevel == WorkLockLevel.None)
            .ToListAsync(ct);
        var workIds = movableWorks.Select(w => w.Id).ToList();

        var now = DateTime.UtcNow;

        if (workIds.Count > 0)
        {
            // Sub-breaks (work pauses) attached to the movable works being replaced.
            // Standalone absence breaks (ParentWorkId == null) are intentionally left intact.
            var subBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && workIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in subBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

            var clonedWorkChanges = await _context.WorkChange
                .Where(wc => !wc.IsDeleted && workIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in clonedWorkChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

            var clonedExpenses = await _context.Expenses
                .Where(e => !e.IsDeleted && workIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in clonedExpenses) { e.IsDeleted = true; e.DeletedTime = now; }
        }

        foreach (var w in movableWorks) { w.IsDeleted = true; w.DeletedTime = now; }
    }

    /// <summary>
    /// Re-points the movable cloned works in place to the agents the harmonised bitmap assigns them to,
    /// instead of delete+recreate. The clone already carries the original work's shift, times and
    /// WorkChange/Expense/sub-Break children (CloneWorks), so changing only the ClientId preserves those
    /// children on accept (C4). The harmonizer reassigns agents within fixed (shift, date) columns, so
    /// the date stays invariant and the children remain consistent. Movable clones the bitmap no longer
    /// assigns are soft-deleted with their children; locked works and absence breaks are never touched.
    /// Returns the ids of the re-pointed works.
    /// </summary>
    internal async Task<IReadOnlyList<Guid>> RepointClonedWorksAsync(
        Guid token,
        DateOnly from,
        DateOnly until,
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken ct)
    {
        var targets = new Dictionary<Guid, (Guid Agent, DateOnly Date)>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            if (!Guid.TryParse(bitmap.Rows[r].Id, out var agentId))
            {
                continue;
            }
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.Symbol == CellSymbol.Free || cell.Symbol == CellSymbol.Break || cell.IsLocked || cell.WorkIds.Count == 0)
                {
                    continue;
                }
                foreach (var realWorkId in cell.WorkIds)
                {
                    if (workIdMap.TryGetValue(realWorkId, out var cloneId))
                    {
                        targets[cloneId] = (agentId, bitmap.Days[d]);
                    }
                }
            }
        }

        var movableWorks = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.CurrentDate >= from && w.CurrentDate <= until
                && !w.IsDeleted && w.LockLevel == WorkLockLevel.None)
            .ToListAsync(ct);

        // Apply each work's new (agent, date) and remember it so the work's children can follow. Setting
        // both ClientId and CurrentDate keeps the result identical to the delete+recreate BuildBulkItems.
        var appliedTargets = new Dictionary<Guid, (Guid Agent, DateOnly Date)>();
        var toDelete = new List<Work>();
        foreach (var w in movableWorks)
        {
            if (targets.TryGetValue(w.Id, out var target)
                || (w.ParentWorkId.HasValue && targets.TryGetValue(w.ParentWorkId.Value, out target)))
            {
                w.ClientId = target.Agent;
                w.CurrentDate = target.Date;
                appliedTargets[w.Id] = target;
            }
            else
            {
                toDelete.Add(w);
            }
        }

        // Sub-breaks (work pauses) carry their OWN ClientId/CurrentDate (ScheduleEntryBase), unlike
        // WorkChange/Expenses which the schedule SP derives from the parent work. Re-point them with their
        // parent so period-hours and any client-keyed aggregation stay attributed to the new agent.
        if (appliedTargets.Count > 0)
        {
            var repointedIds = appliedTargets.Keys.ToList();
            var subBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && repointedIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in subBreaks)
            {
                var target = appliedTargets[b.ParentWorkId!.Value];
                b.ClientId = target.Agent;
                b.CurrentDate = target.Date;
            }
        }

        if (toDelete.Count > 0)
        {
            var now = DateTime.UtcNow;
            var deleteIds = toDelete.Select(w => w.Id).ToList();

            var subBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && deleteIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in subBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

            var workChanges = await _context.WorkChange
                .Where(wc => !wc.IsDeleted && deleteIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

            var expenses = await _context.Expenses
                .Where(e => !e.IsDeleted && deleteIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in expenses) { e.IsDeleted = true; e.DeletedTime = now; }

            foreach (var w in toDelete) { w.IsDeleted = true; w.DeletedTime = now; }
        }

        return appliedTargets.Keys.ToList();
    }

    private List<BulkWorkItem> BuildBulkItems(
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Work> originalWorks,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid analyseToken)
    {
        var items = new List<BulkWorkItem>();
        var unmappedShifts = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var newAgentId = Guid.Parse(bitmap.Rows[r].Id);
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                // Skip Free cells, absence Breaks, and any locked cell. Locked works are preserved
                // in place by DeleteClonedScheduleEntriesAsync and must not be rebuilt unlocked;
                // only movable works are re-materialised from the harmonised bitmap here.
                if (cell.Symbol == CellSymbol.Free || cell.Symbol == CellSymbol.Break || cell.IsLocked || cell.WorkIds.Count == 0)
                {
                    continue;
                }
                foreach (var workId in cell.WorkIds)
                {
                    if (!originalWorks.TryGetValue(workId, out var original))
                    {
                        continue;
                    }

                    // The cloned scenario only contains shifts mapped by CloneScenarioDataAsync.
                    // Falling back to the unmapped main-scenario shift_id sets the new Work's
                    // shift onto a Shift whose analyse_token is null instead of the new scenario
                    // token. get_schedule_entries.sql filters works through filtered_shift_ids
                    // (s.analyse_token IS NOT DISTINCT FROM p_analyse_token) and drops the work,
                    // making the schedule appear empty. Skip such items and surface them via the
                    // log so the missing-clone data issue is diagnosable.
                    if (!shiftIdMap.TryGetValue(original.ShiftId, out var mappedShiftId))
                    {
                        unmappedShifts.Add(original.ShiftId);
                        continue;
                    }

                    items.Add(new BulkWorkItem
                    {
                        ClientId = newAgentId,
                        ShiftId = mappedShiftId,
                        CurrentDate = bitmap.Days[d],
                        StartTime = original.StartTime,
                        EndTime = original.EndTime,
                        WorkTime = original.WorkTime,
                        Surcharges = original.Surcharges,
                        Information = original.Information,
                        AnalyseToken = analyseToken,
                    });
                }
            }
        }

        if (unmappedShifts.Count > 0)
        {
            _logger.LogWarning(
                "HarmonizerApply: {Count} original shifts had no entry in the cloned-scenario shiftIdMap and were skipped: {ShiftIds}. CloneScenarioDataAsync did not clone these shifts into the new scenario.",
                unmappedShifts.Count,
                string.Join(",", unmappedShifts));
        }

        return items;
    }

    private async Task<Guid> ResolveRunGroupIdAsync(Guid? sourceAnalyseToken, CancellationToken ct)
    {
        if (sourceAnalyseToken is not Guid token)
        {
            return Guid.NewGuid();
        }
        var sourceScenario = await _scenarioRepository.GetByTokenAsync(token, ct);
        return sourceScenario?.RunGroupId ?? Guid.NewGuid();
    }

    private async Task<string> GenerateUniqueNameAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken ct,
        string? namePrefixOverride = null)
    {
        var prefix = string.IsNullOrWhiteSpace(namePrefixOverride) ? ScenarioNamePrefix : namePrefixOverride;
        var baseName = $"{prefix} {from:dd.MM.yy} – {until:dd.MM.yy}";
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
