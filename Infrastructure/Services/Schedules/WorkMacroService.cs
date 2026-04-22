// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Processes macro calculations for work and WorkChange entries (surcharges, working time).
/// </summary>
/// <param name="shiftRepository">Loads shift data including MacroId</param>
/// <param name="macroDataProvider">Calculates macro input data from Work/WorkChange</param>
/// <param name="macroCompilationService">Kompiliert und führt Macros aus</param>
/// <param name="context">Database access for WorkChange-to-Work resolution</param>
/// <param name="logger">Logger for warnings and error messages</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class WorkMacroService : IWorkMacroService
{
    private readonly DataBaseContext _context;
    private readonly IShiftRepository _shiftRepository;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;
    private readonly ILogger<WorkMacroService> _logger;

    public WorkMacroService(
        DataBaseContext context,
        IShiftRepository shiftRepository,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService,
        ILogger<WorkMacroService> logger)
    {
        _context = context;
        _shiftRepository = shiftRepository;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
        _logger = logger;
    }

    public async Task ProcessWorkMacroAsync(Work work)
    {
        try
        {
            var shift = await _shiftRepository.Get(work.ShiftId);
            if (shift == null)
            {
                _logger.LogWarning("Shift with ID {ShiftId} not found for Work {WorkId}", work.ShiftId, work.Id);
                CalculateWorkTime(work);
                return;
            }

            CalculateWorkTime(work);

            if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataAsync(work);
            var result = await _macroCompilationService.CompileAndExecuteAsync(shift.MacroId.Value, macroData);

            if (result.Success && result.ResultValue.HasValue)
            {
                work.Surcharges = result.ResultValue.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for Work {WorkId}", work.Id);
        }
    }

    public async Task ProcessWorkChangeMacroAsync(WorkChange workChange)
    {
        try
        {
            CalculateChangeTime(workChange);

            var work = await _context.Work.FirstOrDefaultAsync(w => w.Id == workChange.WorkId);
            if (work == null)
            {
                _logger.LogWarning("Work with ID {WorkId} not found for WorkChange {WorkChangeId}", workChange.WorkId, workChange.Id);
                return;
            }

            var shift = await _shiftRepository.Get(work.ShiftId);
            if (shift == null)
            {
                _logger.LogWarning("Shift with ID {ShiftId} not found for WorkChange {WorkChangeId}", work.ShiftId, workChange.Id);
                return;
            }

            if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataForWorkChangeAsync(workChange, work);
            var result = await _macroCompilationService.CompileAndExecuteAsync(shift.MacroId.Value, macroData);

            if (result.Success && result.ResultValue.HasValue)
            {
                workChange.Surcharges = result.ResultValue.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for WorkChange {WorkChangeId}", workChange.Id);
        }
    }

    private static void CalculateWorkTime(Work work)
    {
        var start = work.StartTime.ToTimeSpan();
        var end = work.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        work.WorkTime = (decimal)duration.TotalHours;
    }

    private static void CalculateChangeTime(WorkChange workChange)
    {
        if (workChange.Type != WorkChangeType.TravelWithin && workChange.Type != WorkChangeType.ReplacementWithin)
        {
            return;
        }

        var start = workChange.StartTime.ToTimeSpan();
        var end = workChange.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        workChange.ChangeTime = (decimal)duration.TotalHours;
    }
}
