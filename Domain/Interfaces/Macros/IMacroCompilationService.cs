// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

/// <summary>
/// Encapsulates macro loading, compilation, import setting and execution.
/// </summary>
/// <param name="macroId">The ID of the macro to execute</param>
/// <param name="macroData">The calculated macro input data (hours, weekday, holiday etc.)</param>
public interface IMacroCompilationService
{
    Task<MacroExecutionResult> CompileAndExecuteAsync(Guid macroId, MacroData macroData);
}
