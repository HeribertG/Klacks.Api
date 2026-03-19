// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

/// <summary>
/// Kapselt Macro-Laden, Kompilierung, Import-Setzung und Ausführung.
/// </summary>
/// <param name="macroId">Die ID des auszuführenden Macros</param>
/// <param name="macroData">Die berechneten Macro-Eingabedaten (Stunden, Wochentag, Feiertag etc.)</param>
public interface IMacroCompilationService
{
    Task<MacroExecutionResult> CompileAndExecuteAsync(Guid macroId, MacroData macroData);
}
