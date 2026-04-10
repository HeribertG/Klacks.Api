// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages updating container work children: collection sync, macro execution for sub-works/breaks/work-changes,
/// unpaid break surcharge negation, and parent work time/surcharge recalculation.
/// </summary>
/// <param name="workId">The parent container work ID</param>
/// <param name="updatedWorks">Sub-work entities to sync</param>
/// <param name="updatedBreaks">Sub-break entities to sync</param>
/// <param name="updatedWorkChanges">Work change entities to sync</param>

using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ContainerWorkChildrenManager : IContainerWorkChildrenManager
{
    private readonly DataBaseContext _context;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;

    public ContainerWorkChildrenManager(
        DataBaseContext context,
        EntityCollectionUpdateService collectionUpdateService,
        IWorkMacroService workMacroService,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService)
    {
        _context = context;
        _collectionUpdateService = collectionUpdateService;
        _workMacroService = workMacroService;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
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

        UpdateParentProperties(parentWork, parentStartBase, parentEndBase, parentStartTime, parentEndTime);
        InheritParentDefaults(parentWork, updatedBreaks);

        await SyncCollections(workId, updatedWorks, updatedBreaks, updatedWorkChanges, cancellationToken);

        await ProcessSubWorkMacrosAsync(updatedWorks);
        await ProcessParentWorkCalculationsAsync(parentWork, updatedBreaks, cancellationToken);
        await ProcessWorkChangeMacrosAsync(updatedWorkChanges);

        return parentWork;
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
            b.ClientId = parentWork.ClientId;
            if (b.CurrentDate == default)
            {
                b.CurrentDate = parentWork.CurrentDate;
            }
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

    private async Task ProcessSubWorkMacrosAsync(List<Work> updatedWorks)
    {
        foreach (var subWork in updatedWorks)
        {
            await _workMacroService.ProcessWorkMacroAsync(subWork);
        }
    }

    private async Task ProcessParentWorkCalculationsAsync(
        Work? parentWork,
        List<Break> updatedBreaks,
        CancellationToken cancellationToken)
    {
        if (parentWork == null) return;

        var unpaidSet = await GetUnpaidAbsenceIdsAsync(updatedBreaks, cancellationToken);

        var unpaidBreakSpans = updatedBreaks
            .Where(b => unpaidSet.Contains(b.AbsenceId))
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        parentWork.WorkTime = ContainerWorkTimeCalculator.CalculatePaidHours(
            parentWork.StartTime, parentWork.EndTime, unpaidBreakSpans);

        await _workMacroService.ProcessWorkMacroAsync(parentWork);
        var parentSurcharges = parentWork.Surcharges;

        var parentShift = await _context.Shift
            .FirstOrDefaultAsync(s => s.Id == parentWork.ShiftId, cancellationToken);

        foreach (var subBreak in updatedBreaks.Where(b => unpaidSet.Contains(b.AbsenceId)))
        {
            await ProcessUnpaidBreakSurchargesAsync(subBreak, parentWork, parentShift);
            parentSurcharges += subBreak.Surcharges;
        }

        parentWork.Surcharges = parentSurcharges;
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

    private async Task ProcessUnpaidBreakSurchargesAsync(Break subBreak, Work parentWork, Shift? parentShift)
    {
        var breakDurationHours = CalculateBreakDurationHours(subBreak.StartTime, subBreak.EndTime);
        subBreak.WorkTime = -breakDurationHours;
        subBreak.Surcharges = 0;

        if (parentShift?.MacroId == null || parentShift.MacroId.Value == Guid.Empty)
        {
            return;
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

        if (result.Success && result.ResultValue.HasValue)
        {
            subBreak.Surcharges = -result.ResultValue.Value;
        }
    }

    private async Task ProcessWorkChangeMacrosAsync(List<WorkChange> updatedWorkChanges)
    {
        foreach (var wc in updatedWorkChanges)
        {
            await _workMacroService.ProcessWorkChangeMacroAsync(wc);
        }
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
