// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting;

public sealed class ScriptResult
{
    public bool Success { get; init; }
    public List<ResultMessage> Messages { get; init; } = new();
    public ScriptError? Error { get; init; }

    public static ScriptResult Ok(List<ResultMessage> messages) => new()
    {
        Success = true,
        Messages = messages
    };

    public static ScriptResult Fail(ScriptError error, List<ResultMessage>? messages = null) => new()
    {
        Success = false,
        Error = error,
        Messages = messages ?? new()
    };
}
