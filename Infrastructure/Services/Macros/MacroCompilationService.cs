// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Macros;

/// <summary>
/// Encapsulates macro loading, compilation (with cache), import setting and execution. The OUTPUT messages of a run are
/// read by <see cref="MacroResultAggregator"/>, the one reader shared with the macro dry-run.
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

        return MacroResultAggregator.Aggregate(results);
    }
}
