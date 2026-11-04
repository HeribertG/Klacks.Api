// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Manages updating container work children: collection sync, macro execution for sub-works/breaks/work-changes,
/// unpaid break negation, and the parent's own macro run. The work keeps the hours and surcharges of its whole span from its
/// own macro; an unpaid sub-break carries its negative hours and surcharges on the break row only, because the period sums
/// add both rows (taking it off the work as well would count it twice). Every save checks that the container holds all of
/// its children, their travel, briefing and debriefing times included (ContainerFitCheck), with the bounds the save leaves.
/// </summary>
/// <param name="workId">The parent container work ID</param>
/// <param name="updatedWorks">Sub-work entities to sync</param>
/// <param name="updatedBreaks">Sub-break entities to sync</param>
/// <param name="updatedWorkChanges">Work change entities to sync</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ContainerWorkChildrenManager : IContainerWorkChildrenManager
{
    private readonly DataBaseContext _context;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;
    private readonly ILogger<ContainerWorkChildrenManager> _logger;

    public ContainerWorkChildrenManager(
        DataBaseContext context,
        EntityCollectionUpdateService collectionUpdateService,
        IWorkMacroService workMacroService,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService,
        ILogger<ContainerWorkChildrenManager> logger)
    {
        _context = context;
        _collectionUpdateService = collectionUpdateService;
        _workMacroService = workMacroService;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
        _logger = logger;
    }

    public async Task<Work?> UpdateChildrenAsync(
        Guid workId,
        string? parentStartBase,
        string? parentEndBase,
        TimeOnly? parentStartTime,
        TimeOnly? parentEndTime,
        List<Work> updatedWorks,
        List<Break> updatedBreaks,
        List<WorkChange> updatedWorkChanges,
        CancellationToken cancellationToken)
    {
        var parentWork = await _context.Work
            .FirstOrDefaultAsync(w => w.Id == workId && !w.IsDeleted, cancellationToken);

        EnsureChildrenInsideBounds(parentWork, parentStartTime, parentEndTime, updatedWorks, updatedBreaks, updatedWorkChanges);
        UpdateParentProperties(parentWork, parentStartBase, parentEndBase, parentStartTime, parentEndTime);
        InheritParentDefaults(parentWork, updatedBreaks);

        await SyncCollections(workId, updatedWorks, updatedBreaks, updatedWorkChanges, cancellationToken);

        await ProcessSubWorkMacrosAsync(updatedWorks);
        await ProcessParentWorkCalculationsAsync(parentWork, updatedBreaks, cancellationToken);
        await ProcessWorkChangeMacrosAsync(updatedWorkChanges);

        return parentWork;
    }

    public async Task<bool> RecalculateForNewOwnerAsync(Work parentWork, CancellationToken cancellationToken)
    {
        var allSubWorks = await _context.Work
            .Where(w => w.ParentWorkId == parentWork.Id && !w.IsDeleted)
            .ToListAsync(cancellationToken);
        var subWorks = allSubWorks.Where(w => w.ClientId == parentWork.ClientId).ToList();
        var subBreaks = await _context.Break
            .Where(b => b.ParentWorkId == parentWork.Id && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var workIds = subWorks.Select(w => w.Id).Append(parentWork.Id).ToList();
        var workChanges = await _context.WorkChange
            .Where(wc => workIds.Contains(wc.WorkId) && !wc.IsDeleted)
            .ToListAsync(cancellationToken);

        var subWorksCalculated = await ProcessSubWorkMacrosAsync(subWorks);
        var parentCalculated = await ProcessParentWorkCalculationsAsync(parentWork, subBreaks, cancellationToken);
        var workChangesCalculated = await ProcessWorkChangeMacrosAsync(workChanges);
        return subWorksCalculated && parentCalculated && workChangesCalculated;
    }

    public async Task<bool> CalculateNewSubBreakAsync(Break subBreak, Work parentWork, CancellationToken cancellationToken)
    {
        var unpaid = await GetUnpaidAbsenceIdsAsync([subBreak], cancellationToken);
        if (!unpaid.Contains(subBreak.AbsenceId))
        {
            return true;
        }

        var parentShift = await _context.Shift.FirstOrDefaultAsync(s => s.Id == parentWork.ShiftId, cancellationToken);
        return await ProcessUnpaidBreakSurchargesAsync(subBreak, parentWork, parentShift);
    }

    private static void EnsureChildrenInsideBounds(
        Work? parentWork,
        TimeOnly? startTime,
        TimeOnly? endTime,
        List<Work> updatedWorks,
        List<Break> updatedBreaks,
        List<WorkChange> updatedWorkChanges)
    {
        if (parentWork == null) return;

        ContainerFitCheck.EnsureChildrenFit(
            startTime ?? parentWork.StartTime,
            endTime ?? parentWork.EndTime,
            updatedWorks,
            updatedBreaks,
            updatedWorkChanges);
    }

    private static void UpdateParentProperties(
        Work? parentWork,
        string? startBase,
        string? endBase,
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        if (parentWork == null) return;

        parentWork.StartBase = startBase;
        parentWork.EndBase = endBase;

        if (startTime.HasValue)
        {
            parentWork.StartTime = startTime.Value;
        }

        if (endTime.HasValue)
        {
            parentWork.EndTime = endTime.Value;
        }
    }

    private static void InheritParentDefaults(Work? parentWork, List<Break> updatedBreaks)
    {
        if (parentWork == null) return;

        foreach (var b in updatedBreaks)
        {
            ContainerBreakDefaults.Apply(b, parentWork);
        }
    }

    private async Task SyncCollections(
        Guid workId,
        List<Work> updatedWorks,
        List<Break> updatedBreaks,
        List<WorkChange> updatedWorkChanges,
        CancellationToken cancellationToken)
    {
        var existingWorks = await _context.Work
            .Where(w => w.ParentWorkId == workId && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingBreaks = await _context.Break
            .Where(b => b.ParentWorkId == workId && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingSubWorkIds = existingWorks.Select(w => w.Id).ToList();

        var existingWorkChanges = existingSubWorkIds.Count > 0
            ? await _context.WorkChange
                .Where(wc => existingSubWorkIds.Contains(wc.WorkId))
                .ToListAsync(cancellationToken)
            : new List<WorkChange>();

        _collectionUpdateService.UpdateCollection(
            existingWorks, updatedWorks, workId,
            (work, parentId) => work.ParentWorkId = parentId);

        _collectionUpdateService.UpdateCollection(
            existingBreaks, updatedBreaks, workId,
            (b, parentId) => b.ParentWorkId = parentId);

        SyncWorkChanges(existingWorkChanges, updatedWorkChanges);
    }

    private void SyncWorkChanges(List<WorkChange> existing, List<WorkChange> updated)
    {
        foreach (var existingWc in existing)
        {
            var updatedWc = updated.FirstOrDefault(wc => wc.Id == existingWc.Id);
            if (updatedWc == null)
            {
                _context.Entry(existingWc).State = EntityState.Deleted;
            }
            else
            {
                var entry = _context.Entry(existingWc);
                entry.CurrentValues.SetValues(updatedWc);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newWc in updated.Where(wc => wc.Id == Guid.Empty || !existing.Any(e => e.Id == wc.Id)))
        {
            _context.Entry(newWc).State = EntityState.Added;
        }
    }

    private async Task<bool> ProcessSubWorkMacrosAsync(List<Work> updatedWorks)
    {
        var allCalculated = true;
        foreach (var subWork in updatedWorks)
        {
            allCalculated &= await _workMacroService.ProcessWorkMacroAsync(subWork);
        }

        return allCalculated;
    }

    private async Task<bool> ProcessParentWorkCalculationsAsync(
        Work? parentWork,
        List<Break> updatedBreaks,
        CancellationToken cancellationToken)
    {
        if (parentWork == null) return true;

        var unpaidSet = await GetUnpaidAbsenceIdsAsync(updatedBreaks, cancellationToken);
        var allCalculated = await _workMacroService.ProcessWorkMacroAsync(parentWork);

        var parentShift = await _context.Shift
            .FirstOrDefaultAsync(s => s.Id == parentWork.ShiftId, cancellationToken);

        foreach (var subBreak in updatedBreaks.Where(b => unpaidSet.Contains(b.AbsenceId)))
        {
            allCalculated &= await ProcessUnpaidBreakSurchargesAsync(subBreak, parentWork, parentShift);
        }

        return allCalculated;
    }

    private async Task<HashSet<Guid>> GetUnpaidAbsenceIdsAsync(
        List<Break> updatedBreaks,
        CancellationToken cancellationToken)
    {
        var breakAbsenceIds = updatedBreaks
            .Select(b => b.AbsenceId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (breakAbsenceIds.Count == 0) return new HashSet<Guid>();

        var unpaidAbsenceIds = await _context.Absence
            .Where(a => breakAbsenceIds.Contains(a.Id) && a.IsUnpaid && !a.IsDeleted)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        return unpaidAbsenceIds.ToHashSet();
    }

    private async Task<bool> ProcessUnpaidBreakSurchargesAsync(Break subBreak, Work parentWork, Shift? parentShift)
    {
        var breakDurationHours = CalculateBreakDurationHours(subBreak.StartTime, subBreak.EndTime);
        subBreak.WorkTime = -breakDurationHours;
        subBreak.Surcharges = 0;

        if (parentShift?.MacroId == null || parentShift.MacroId.Value == Guid.Empty)
        {
            return true;
        }

        var tempWork = new Work
        {
            ClientId = parentWork.ClientId,
            CurrentDate = parentWork.CurrentDate,
            ShiftId = parentWork.ShiftId,
            StartTime = subBreak.StartTime,
            EndTime = subBreak.EndTime,
            WorkTime = breakDurationHours
        };

        var macroData = await _macroDataProvider.GetMacroDataAsync(tempWork);
        var result = await _macroCompilationService.CompileAndExecuteAsync(parentShift.MacroId.Value, macroData);

        if (!result.Success)
        {
            _logger.LogError(
                "Macro {MacroId} failed for sub-break {BreakId}; its surcharges stay at zero", parentShift.MacroId.Value, subBreak.Id);
            return false;
        }

        if (result.ResultValue.HasValue)
        {
            subBreak.Surcharges = -result.ResultValue.Value;
            await SurchargeItemSynchronizer.ReplaceAsync(
                _context,
                subBreak.SurchargeItems,
                _context.SurchargeItem.Where(si => si.BreakId == subBreak.Id),
                result.Surcharges,
                -1m);
        }

        return true;
    }

    private async Task<bool> ProcessWorkChangeMacrosAsync(List<WorkChange> updatedWorkChanges)
    {
        var allCalculated = true;
        foreach (var wc in updatedWorkChanges)
        {
            allCalculated &= await _workMacroService.ProcessWorkChangeMacroAsync(wc);
        }

        return allCalculated;
    }

    private static decimal CalculateBreakDurationHours(TimeOnly start, TimeOnly end)
    {
        var startSpan = start.ToTimeSpan();
        var endSpan = end.ToTimeSpan();
        var duration = endSpan >= startSpan
            ? endSpan - startSpan
            : TimeSpan.FromHours(24) - startSpan + endSpan;
        return (decimal)duration.TotalHours;
    }
}
