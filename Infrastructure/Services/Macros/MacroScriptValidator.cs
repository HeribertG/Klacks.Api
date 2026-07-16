// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

/// <summary>
/// Validates macro scripts before they are persisted or imported. Static compilation alone catches
/// almost nothing: the SyntaxAnalyser is empirically so tolerant that garbage like "OUTPUT 1, ((("
/// parses without error, and some malformed input ("DIM 123abc") loops the parser FOREVER.
/// Validation therefore compiles AND probe-executes the script with neutral inputs inside a hard
/// wall-clock budget - a hang or a runtime error fails the validation fast instead of freezing the
/// caller or a later work save. The probe leaks one worker thread when the parser hangs; acceptable
/// for a fail-fast validation path.
/// </summary>
/// <param name="content">The macro script body to validate</param>
public class MacroScriptValidator : IMacroScriptValidator
{
    private const int ValidationTimeoutMs = 5000;
    private const string TimeOfDayProbeValue = "00:00";

    public MacroScriptValidationResult Validate(string content)
    {
        string? failure;
        var probe = Task.Run(() => CompileAndProbeExecute(content));
        try
        {
            if (!probe.Wait(ValidationTimeoutMs))
            {
                return MacroScriptValidationResult.Failure(
                    $"The script did not finish compile/probe within {ValidationTimeoutMs} ms - the script parser loops on malformed input; fix the script.");
            }

            failure = probe.Result;
        }
        catch (AggregateException ex)
        {
            return MacroScriptValidationResult.Failure(ex.InnerException?.Message ?? ex.Message);
        }

        return failure == null
            ? MacroScriptValidationResult.Success()
            : MacroScriptValidationResult.Failure(failure);
    }

    private static string? CompileAndProbeExecute(string content)
    {
        var compiled = CompiledScript.Compile(content);
        if (compiled.HasError)
        {
            return $"compile error: {compiled.Error?.Description}";
        }

        foreach (var symbolName in compiled.ExternalSymbols.Keys)
        {
            compiled.SetExternalValue(symbolName, ProbeDefaultFor(symbolName));
        }

        var context = new ScriptExecutionContext(compiled);
        var result = context.Execute();
        return result.Success ? null : $"runtime error: {result.Error?.Description}";
    }

    // Neutral probe inputs: the well-known time-of-day imports get a parsable "00:00", everything else
    // gets zero - enough to drive the common macro shapes through one execution without asserting any
    // business result.
    private static object ProbeDefaultFor(string symbolName)
    {
        var normalized = symbolName.ToLowerInvariant();
        return normalized is "fromhour" or "untilhour" or "nightstart" or "nightend"
            ? TimeOfDayProbeValue
            : 0m;
    }
}
