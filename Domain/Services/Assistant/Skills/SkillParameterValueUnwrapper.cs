// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Unwraps skill invocation argument values that arrive as JsonElement (from tool-call JSON
/// deserialization into Dictionary&lt;string, object&gt;) into plain CLR values, so parameter
/// validation and parameter extraction share one canonical unwrap.
/// </summary>
/// <param name="value">Raw argument value: a JsonElement or an already-CLR value.</param>

using System.Text.Json;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillParameterValueUnwrapper
{
    public static object? Unwrap(object? value) =>
        value is JsonElement jsonElement ? UnwrapJsonElement(jsonElement) : value;

    public static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
