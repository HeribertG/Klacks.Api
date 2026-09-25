// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Interfaces;

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
        MacroDataImportBinder.Bind(compiledScript, macroData);

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
        var surcharges = new List<MacroSurchargeItem>();
        foreach (var msg in results)
        {
            if (!decimal.TryParse(msg.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                continue;
            }

            if (msg.Type == (int)MacroTypeEnum.DefaultResult)
            {
                resultValue = parsed;
            }
            else if (parsed != 0m && TryMapSurchargeType(msg.Type, out var surchargeType))
            {
                surcharges.Add(new MacroSurchargeItem(surchargeType, parsed));
            }
        }

        return new MacroExecutionResult(true, resultValue, surcharges);
    }

    private static bool TryMapSurchargeType(int messageType, out SurchargeType surchargeType)
    {
        switch ((MacroTypeEnum)messageType)
        {
            case MacroTypeEnum.SurchargeNight:
                surchargeType = SurchargeType.Night;
                return true;
            case MacroTypeEnum.SurchargeWeekend1:
                surchargeType = SurchargeType.Weekend1;
                return true;
            case MacroTypeEnum.SurchargeWeekend2:
                surchargeType = SurchargeType.Weekend2;
                return true;
            case MacroTypeEnum.SurchargeWeekend3:
                surchargeType = SurchargeType.Weekend3;
                return true;
            case MacroTypeEnum.SurchargeHoliday:
                surchargeType = SurchargeType.Holiday;
                return true;
            default:
                surchargeType = default;
                return false;
        }
    }
}
