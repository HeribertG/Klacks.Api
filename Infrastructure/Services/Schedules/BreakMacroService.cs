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

public class BreakMacroService : IBreakMacroService
{
    private readonly DataBaseContext _context;
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMacroCache _macroCache;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroEngine _macroEngine;
    private readonly ILogger<BreakMacroService> _logger;

    public BreakMacroService(
        DataBaseContext context,
        IMacroManagementService macroManagementService,
        IMacroCache macroCache,
        IMacroDataProvider macroDataProvider,
        IMacroEngine macroEngine,
        ILogger<BreakMacroService> logger)
    {
        _context = context;
        _macroManagementService = macroManagementService;
        _macroCache = macroCache;
        _macroDataProvider = macroDataProvider;
        _macroEngine = macroEngine;
        _logger = logger;
    }

    public async Task ProcessBreakMacroAsync(Break breakEntry)
    {
        try
        {
            var absence = await _context.Absence.FirstOrDefaultAsync(a => a.Id == breakEntry.AbsenceId);
            if (absence?.MacroId == null || absence.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macro = await _macroManagementService.GetMacroAsync(absence.MacroId.Value);
            if (macro == null)
            {
                _logger.LogWarning("Macro with ID {MacroId} not found for Absence {AbsenceId}", absence.MacroId.Value, absence.Id);
                return;
            }

            var cachedScript = _macroCache.GetOrCompile(macro.Id, macro.Content);
            if (cachedScript.HasError)
            {
                _logger.LogError(
                    "Macro compilation failed for Break {BreakId}, Macro {MacroName}: {Error}",
                    breakEntry.Id,
                    macro.Name,
                    cachedScript.Error?.Description);
                return;
            }

            var compiledScript = cachedScript.CloneForExecution();
            var macroData = await _macroDataProvider.GetMacroDataForBreakAsync(breakEntry);

            SetImportsFromMacroData(compiledScript, macroData);

            var results = _macroEngine.RunWithScript(compiledScript);

            if (_macroEngine.ErrorNumber != 0)
            {
                _logger.LogError(
                    "Macro execution failed for Break {BreakId}, Macro {MacroName}: ErrorNumber={ErrorNumber}, ErrorCode={ErrorCode}",
                    breakEntry.Id,
                    macro.Name,
                    _macroEngine.ErrorNumber,
                    _macroEngine.ErrorCode);
                return;
            }

            foreach (var msg in results)
            {
                if (msg.Type == (int)MacroTypeEnum.DefaultResult &&
                    decimal.TryParse(msg.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var workTime))
                {
                    breakEntry.WorkTime = workTime;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for Break {BreakId}", breakEntry.Id);
        }
    }

    private static void SetImportsFromMacroData(CompiledScript compiledScript, MacroData data)
    {
        compiledScript.SetExternalValue("hour", 0);
        compiledScript.SetExternalValue("fromhour", string.Empty);
        compiledScript.SetExternalValue("untilhour", string.Empty);
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

    public async Task ReprocessAllBreaksAsync(DateOnly startDate, DateOnly endDate, List<Guid>? clientIds = null)
    {
        var query = _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate);

        if (clientIds is { Count: > 0 })
        {
            query = query.Where(b => clientIds.Contains(b.ClientId));
        }

        var breaks = await query.ToListAsync();

        _logger.LogInformation("Reprocessing macros for {Count} breaks from {Start} to {End}", breaks.Count, startDate, endDate);

        foreach (var breakEntry in breaks)
        {
            await ProcessBreakMacroAsync(breakEntry);
        }

        await _context.SaveChangesAsync();
    }

}
