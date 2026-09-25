// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one rule that decides whether a macro belongs to the assistant: created by it (Assistant) or stored by it
/// as an extended copy of another macro (AssistantExtension).
/// </summary>
/// <param name="origin">The persisted origin of the macro</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Extensions;

public static class MacroOriginExtensions
{
    public static bool IsAssistantOwned(this MacroOrigin origin) =>
        origin is MacroOrigin.Assistant or MacroOrigin.AssistantExtension;
}
