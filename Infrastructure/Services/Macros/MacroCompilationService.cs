// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

/// <summary>
/// Encapsulates macro loading, compilation (with cache), import setting and execution.
/// </summary>
/// <param name="macroManagementService">Loads macro definitions from the database</param>
/// <param name="macroCache">Cache for already compiled macros</param>
/// <param name="macroEngine">Scripting engine for macro execution</param>
/// <param name="logger">Logger for compilation and execution errors</param>
public class MacroCompilationService : IMacroCompilationService
{
    private readonly IMacroManagementService _macroManagementService;
    private readonly IMacroCache _macroCache;
    private readonly IMacroEngine _macroEngine;
    private readonly ILogger<MacroCompilationService> _logger;

    public MacroCompilationService(
        IMacroManagementService macroManagementService,
        IMacroCache macroCache,
        IMacroEngine macroEngine,
        ILogger<MacroCompilationService> logger)
    {
        _macroManagementService = macroManagementService;
        _macroCache = macroCache;
        _macroEngine = macroEngine;
        _logger = logger;
    }

    public async Task<MacroExecutionResult> CompileAndExecuteAsync(Guid macroId, MacroData macroData)
    {
        var macro = await _macroManagementService.GetMacroAsync(macroId);
        if (macro == null)
        {
            _logger.LogWarning("Macro with ID {MacroId} not found", macroId);
            return new MacroExecutionResult(false, null);
        }

        var cachedScript = _macroCache.GetOrCompile(macro.Id, macro.Content);
        if (cachedScript.HasError)
        {
            _logger.LogError(
                "Macro compilation failed for Macro {MacroName}: {Error}",
                macro.Name,
                cachedScript.Error?.Description);
            return new MacroExecutionResult(false, null);
        }

        var compiledScript = cachedScript.CloneForExecution();
        SetImportsFromMacroData(compiledScript, macroData);

        var results = _macroEngine.RunWithScript(compiledScript);

        if (_macroEngine.ErrorNumber != 0)
        {
            _logger.LogError(
                "Macro execution failed for Macro {MacroName}: ErrorNumber={ErrorNumber}, ErrorCode={ErrorCode}",
                macro.Name,
                _macroEngine.ErrorNumber,
                _macroEngine.ErrorCode);
            return new MacroExecutionResult(false, null);
        }

        decimal? resultValue = null;
        foreach (var msg in results)
        {
            if (msg.Type == (int)MacroTypeEnum.DefaultResult &&
                decimal.TryParse(msg.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                resultValue = parsed;
            }
        }

        return new MacroExecutionResult(true, resultValue);
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
}
