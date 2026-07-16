// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

/// <summary>
/// Validates a macro script by compiling and probe-executing it inside a hard wall-clock budget,
/// so malformed input that hangs the script parser is rejected instead of freezing the caller.
/// </summary>
/// <param name="content">The macro script body to validate</param>
public interface IMacroScriptValidator
{
    MacroScriptValidationResult Validate(string content);
}
