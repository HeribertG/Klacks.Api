// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Scripting;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class WorkMacroService : IWorkMacroService
{
    private readonly DataBaseContext _context;
    private readonly IShiftRepository _shiftRepository;
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMacroCache _macroCache;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroEngine _macroEngine;
    private readonly ILogger<WorkMacroService> _logger;

    public WorkMacroService(
        DataBaseContext context,
        IShiftRepository shiftRepository,
        IMacroManagementService macroManagementService,
        IMacroCache macroCache,
        IMacroDataProvider macroDataProvider,
        IMacroEngine macroEngine,
        ILogger<WorkMacroService> logger)
    {
        _context = context;
        _shiftRepository = shiftRepository;
        _macroManagementService = macroManagementService;
        _macroCache = macroCache;
        _macroDataProvider = macroDataProvider;
        _macroEngine = macroEngine;
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

            var macro = await _macroManagementService.GetMacroAsync(shift.MacroId.Value);
            if (macro == null)
            {
                _logger.LogWarning("Macro with ID {MacroId} not found for Shift {ShiftId}", shift.MacroId.Value, shift.Id);
                return;
            }

            var compiledScript = _macroCache.GetOrCompile(macro.Id, macro.Content);
            if (compiledScript.HasError)
            {
                _logger.LogError(
                    "Macro compilation failed for Work {WorkId}, Macro {MacroName}: {Error}",
                    work.Id,
                    macro.Name,
                    compiledScript.Error?.Description);
                return;
            }

            _logger.LogInformation(
                "Macro compiled successfully. Instructions: {Count}, ExternalSymbols: {Symbols}",
                compiledScript.Instructions.Count,
                string.Join(", ", compiledScript.ExternalSymbols.Keys));

            var macroData = await _macroDataProvider.GetMacroDataAsync(work);

            _logger.LogInformation(
                "MacroData: Hour={Hour}, FromHour={FromHour}, UntilHour={UntilHour}, Weekday={Weekday}, Holiday={Holiday}",
                macroData.Hour, macroData.FromHour, macroData.UntilHour, macroData.Weekday, macroData.Holiday);

            SetImportsFromMacroData(compiledScript, macroData);

            var results = _macroEngine.RunWithScript(compiledScript);

            if (_macroEngine.ErrorNumber != 0)
            {
                _logger.LogError(
                    "Macro execution failed for Work {WorkId}, Macro {MacroName}: ErrorNumber={ErrorNumber}, ErrorCode={ErrorCode}",
                    work.Id,
                    macro.Name,
                    _macroEngine.ErrorNumber,
                    _macroEngine.ErrorCode);
                return;
            }

            foreach (var msg in results)
            {
                if (msg.Type == (int)MacroTypeEnum.DefaultResult &&
                    decimal.TryParse(msg.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var surcharges))
                {
                    work.Surcharges = surcharges;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for Work {WorkId}", work.Id);
        }
    }

    private static void SetImportsFromMacroData(CompiledScript compiledScript, MacroData data)
    {
        compiledScript.SetExternalValue("hour", data.Hour);
        compiledScript.SetExternalValue("fromhour", data.FromHour);
        compiledScript.SetExternalValue("untilhour", data.UntilHour);
        compiledScript.SetExternalValue("weekday", data.Weekday);
        compiledScript.SetExternalValue("holiday", data.Holiday ? 1 : 0);
        compiledScript.SetExternalValue("holidaynextday", data.HolidayNextDay ? 1 : 0);
        compiledScript.SetExternalValue("nightrate", data.NightRate);
        compiledScript.SetExternalValue("holidayrate", data.HolidayRate);
        compiledScript.SetExternalValue("sarate", data.SaRate);
        compiledScript.SetExternalValue("sorate", data.SoRate);
        compiledScript.SetExternalValue("guaranteedhours", data.GuaranteedHours);
        compiledScript.SetExternalValue("fulltime", data.FullTime);
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

            var macro = await _macroManagementService.GetMacroAsync(shift.MacroId.Value);
            if (macro == null)
            {
                _logger.LogWarning("Macro with ID {MacroId} not found for Shift {ShiftId}", shift.MacroId.Value, shift.Id);
                return;
            }

            var compiledScript = _macroCache.GetOrCompile(macro.Id, macro.Content);
            if (compiledScript.HasError)
            {
                _logger.LogError(
                    "Macro compilation failed for WorkChange {WorkChangeId}, Macro {MacroName}: {Error}",
                    workChange.Id,
                    macro.Name,
                    compiledScript.Error?.Description);
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataForWorkChangeAsync(workChange, work);

            _logger.LogInformation(
                "MacroData for WorkChange: Hour={Hour}, FromHour={FromHour}, UntilHour={UntilHour}, Weekday={Weekday}, Holiday={Holiday}",
                macroData.Hour, macroData.FromHour, macroData.UntilHour, macroData.Weekday, macroData.Holiday);

            SetImportsFromMacroData(compiledScript, macroData);

            var results = _macroEngine.RunWithScript(compiledScript);

            if (_macroEngine.ErrorNumber != 0)
            {
                _logger.LogError(
                    "Macro execution failed for WorkChange {WorkChangeId}, Macro {MacroName}: ErrorNumber={ErrorNumber}, ErrorCode={ErrorCode}",
                    workChange.Id,
                    macro.Name,
                    _macroEngine.ErrorNumber,
                    _macroEngine.ErrorCode);
                return;
            }

            foreach (var msg in results)
            {
                if (msg.Type == (int)MacroTypeEnum.DefaultResult &&
                    decimal.TryParse(msg.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var surcharges))
                {
                    workChange.Surcharges = surcharges;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for WorkChange {WorkChangeId}", workChange.Id);
        }
    }

    private static void CalculateChangeTime(WorkChange workChange)
    {
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
