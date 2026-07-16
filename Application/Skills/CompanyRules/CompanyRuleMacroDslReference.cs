// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compact English reference for the macro DSL, surfaced by start_company_rule when the rule kind is
/// customMacro so the model can author a valid script. Lists the statement syntax, the IMPORT symbols the
/// runtime injects (see MacroCompilationService.SetImportsFromMacroData) and the OUTPUT channels
/// (see MacroTypeEnum). Kept as a constant so it never drifts from the runtime.
/// </summary>
namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleMacroDslReference
{
    public const string Reference =
        "Macro DSL reference (English):\n" +
        "Statements: DIM <var> declares a variable; IF <cond> ... ELSE ... END IF; FOR <var> = a TO b ... NEXT; " +
        "DO ... LOOP; FUNCTION <name>(...) ... END FUNCTION; OUTPUT <channel>, <value> emits a result on a channel; " +
        "IMPORT <symbol> reads an injected runtime value.\n" +
        "Built-in helpers: TimeToHours(<time>) converts a time to decimal hours; " +
        "TimeOverlap(startA, endA, startB, endB) returns the overlapping hours of two intervals.\n" +
        "IMPORT symbols (read-only, injected per work row): hour, fromhour, untilhour, weekday, holiday (1/0), " +
        "holidaynextday (1/0), nightrate, holidayrate, we1rate, we2rate, we3rate, nightstart, nightend, " +
        "guaranteedhours, fulltime, weekendday1, weekendday2, weekendday3.\n" +
        "OUTPUT channels: 1 = default result, 5 = info, 10 = night surcharge, 11 = weekend-1 surcharge, " +
        "12 = weekend-2 surcharge, 13 = weekend-3 surcharge, 14 = holiday surcharge.\n" +
        "Example: OUTPUT 1, TimeToHours(untilhour) - TimeToHours(fromhour)";
}
