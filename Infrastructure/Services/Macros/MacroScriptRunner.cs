// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compiles and runs a macro script outside the production engine, for the checks that must neither persist anything nor
/// hold a thread. Compiling turns the compiler crash on a comment in the last line (without a line break after it) into
/// an ordinary compile error with a fixed message. A run binds its inputs through <see cref="MacroDataImportBinder"/> on a
/// fresh execution clone and executes on the calling thread under the given token, which the interpreter checks after
/// every instruction; any exception of a run is reported as its error. Shared by the regression check and the dry-run.
/// </summary>
/// <param name="content">The macro script to compile</param>
/// <param name="compiled">A compiled script; every run works on its own execution clone</param>
/// <param name="data">The inputs of one run</param>
/// <param name="cancellationToken">Budget or caller token checked after every instruction</param>

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public static class MacroScriptRunner
{
    public const string TrailingCommentCompileError =
        "it ends with a comment on its last line without a line break after it, which the script parser cannot handle.";

    public static (CompiledScript? Script, string? Error) TryCompile(string content)
    {
        try
        {
            var compiled = CompiledScript.Compile(content);
            return compiled.HasError ? (null, compiled.Error?.Description) : (compiled, null);
        }
        catch (ArgumentOutOfRangeException)
        {
            return (null, TrailingCommentCompileError);
        }
    }

    public static MacroScriptRun Run(CompiledScript compiled, MacroData data, CancellationToken cancellationToken)
    {
        try
        {
            var script = compiled.CloneForExecution();
            MacroDataImportBinder.Bind(script, data);
            var result = new ScriptExecutionContext(script).Execute(cancellationToken);
            return result.Success
                ? MacroScriptRun.Completed(result.Messages)
                : MacroScriptRun.Failed(result.Error?.Description);
        }
        catch (Exception ex)
        {
            return MacroScriptRun.Failed(ex.Message);
        }
    }
}
