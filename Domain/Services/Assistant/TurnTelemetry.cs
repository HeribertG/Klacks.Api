// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How a chat turn reports about itself: the turn age carried by status chunks, the short correlation id
/// written into log lines and the function-name list stored in the usage row.
/// </summary>

using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class TurnTelemetry
{
    private const int FunctionsCalledMaxLength = 200;
    private const string EmptyFunctionList = "[]";

    // Milliseconds since the turn clock started, i.e. before the toolset assembly the caller already
    // paid for. Null when no clock was handed in, which keeps elapsedMs off the wire instead of
    // reporting an age measured from an unrelated zero point.
    internal static long? ElapsedMsFor(LLMContext context)
    {
        return context.TurnStartTimestamp is { } start
            ? (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds
            : null;
    }

    // The turn's short correlation id: the same value the ambient TurnCorrelation carries into the
    // retrieval log, so both sides of a turn can be joined without threading an id through the layers.
    internal static string TurnCorrelationFor(LLMContext context)
    {
        return context.TurnId is { } turnId ? TurnCorrelation.Format(turnId) : TurnCorrelation.CurrentOrNone;
    }

    // W1.7: fills llm_usage.functions_called with the distinct function names this turn actually
    // invoked, as a JSON array capped to the varchar(200) column. Truncation keeps the row writable;
    // the column is a plain string, so a truncated tail is acceptable for SQL analysis.
    internal static string SerializeFunctionsCalled(IReadOnlyList<LLMFunctionCall> allFunctionCalls)
    {
        var names = allFunctionCalls
            .Where(c => !c.IsRejectedWithoutRunning)
            .Select(c => c.FunctionName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return EmptyFunctionList;
        }

        var json = JsonSerializer.Serialize(names);
        if (json.Length <= FunctionsCalledMaxLength)
        {
            return json;
        }

        while (names.Count > 1 && JsonSerializer.Serialize(names).Length > FunctionsCalledMaxLength)
        {
            names.RemoveAt(names.Count - 1);
        }

        json = JsonSerializer.Serialize(names);
        return json.Length <= FunctionsCalledMaxLength ? json : json[..FunctionsCalledMaxLength];
    }
}
