// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Ergebnis einer Macro-Ausführung mit optionalem Surcharges/WorkTime-Wert.
/// </summary>
/// <param name="Success">Gibt an, ob die Macro-Ausführung erfolgreich war</param>
/// <param name="ResultValue">Der extrahierte Dezimalwert aus dem DefaultResult-Ergebnis, oder null wenn keiner vorhanden</param>
public record MacroExecutionResult(bool Success, decimal? ResultValue);
