// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of one macro script run: the OUTPUT messages when it completed, otherwise the error description.
/// </summary>
/// <param name="Messages">OUTPUT messages in emission order; null when the run failed</param>
/// <param name="Error">Why the run failed; null when it completed</param>

using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public record MacroScriptRun(IReadOnlyList<ResultMessage>? Messages, string? Error)
{
    public bool IsCompleted => Messages != null;

    public static MacroScriptRun Completed(IReadOnlyList<ResultMessage> messages) => new(messages, null);

    public static MacroScriptRun Failed(string? error) => new(null, error);
}
