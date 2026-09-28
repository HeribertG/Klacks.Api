// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Result of validating a macro script by compiling and probe-executing it.
/// </summary>
/// <param name="IsValid">Indicates whether the script compiled and probe-executed without errors</param>
/// <param name="ErrorMessage">Human-readable failure description (compile error, runtime error or parser hang); null when the script is valid</param>
public record MacroScriptValidationResult(bool IsValid, string? ErrorMessage)
{
    public static MacroScriptValidationResult Success() => new(true, null);

    public static MacroScriptValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
