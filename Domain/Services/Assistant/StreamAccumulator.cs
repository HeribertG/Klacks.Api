// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Accumulates streaming tokens and tool call deltas from LLM provider responses.
/// @param _contentBuffer - Accumulated text content
/// @param _toolCalls - Dictionary of tool call index to accumulated name + arguments
/// </summary>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class StreamAccumulator
{
    private readonly StringBuilder _contentBuffer = new();
    private readonly Dictionary<int, (StringBuilder Name, StringBuilder Arguments)> _toolCalls = new();
    private readonly List<LLMFunctionCall> _finalizedCalls = new();

    public string AccumulatedContent => _contentBuffer.ToString();
    public IReadOnlyList<LLMFunctionCall> FunctionCalls => _finalizedCalls;
    public bool HasFunctionCalls => _finalizedCalls.Count > 0;

    public void AppendContent(string text)
    {
        _contentBuffer.Append(text);
    }

    public void AppendToolCallDelta(int index, string? name, string? arguments)
    {
        if (!_toolCalls.ContainsKey(index))
        {
            _toolCalls[index] = (new StringBuilder(), new StringBuilder());
        }

        var (nameBuf, argsBuf) = _toolCalls[index];

        if (!string.IsNullOrEmpty(name))
        {
            nameBuf.Append(name);
        }

        if (!string.IsNullOrEmpty(arguments))
        {
            argsBuf.Append(arguments);
        }
    }

    public void AppendFunctionCallDelta(string? name, string? arguments)
    {
        AppendToolCallDelta(0, name, arguments);
    }

    public void FinalizeFunctionCalls()
    {
        foreach (var (_, (nameBuf, argsBuf)) in _toolCalls.OrderBy(kv => kv.Key))
        {
            var functionName = nameBuf.ToString();
            if (string.IsNullOrEmpty(functionName)) continue;

            var argsString = argsBuf.ToString();
            Dictionary<string, object> parameters;

            try
            {
                parameters = string.IsNullOrEmpty(argsString)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(argsString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? new Dictionary<string, object>();
            }
            catch
            {
                parameters = new Dictionary<string, object>();
            }

            _finalizedCalls.Add(new LLMFunctionCall
            {
                FunctionName = functionName,
                Parameters = parameters
            });
        }
    }

    public void Reset()
    {
        _contentBuffer.Clear();
        _toolCalls.Clear();
        _finalizedCalls.Clear();
    }
}
