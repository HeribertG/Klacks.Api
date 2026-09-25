// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// The phase a turn was in when it was stopped or cut off, recorded with the trajectory so an interrupted
/// turn can be told apart from a finished one and read for what it had achieved.
/// </summary>
public static class InterruptedTurnPhases
{
    /// <summary>Nothing was streamed and no tool was called yet.</summary>
    public const string BeforeText = "before_text";

    /// <summary>Tools were called and no text has been streamed since.</summary>
    public const string DuringTools = "during_tools";

    /// <summary>The answer was being streamed.</summary>
    public const string DuringText = "during_text";
}
