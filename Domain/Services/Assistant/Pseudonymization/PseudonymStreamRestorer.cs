// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Return path for a streamed answer. Content chunks pass through with their real values restored, except a tail that
/// could still grow into a token ("M", "MA-00"): it is held back until the next chunk decides it, at most
/// <see cref="PseudonymizationConstants.MaxStreamHoldbackChars"/> characters, and released at the end. Tool-call deltas
/// are held until the tool-call end marker (or the end of the stream); then each call index is emitted as exactly one
/// delta with its complete, restored argument JSON. The other properties of the first delta of an index are kept.
/// </summary>
/// <param name="map">Dictionary and pattern numbers of the request</param>
/// <param name="unknownTokens">Collects canonical tokens that do not exist</param>

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed partial class PseudonymStreamRestorer
{
    private const string IndexProperty = "index";
    private const string NameProperty = "name";
    private const string ArgumentsProperty = "arguments";

    private readonly PseudonymMap _map;
    private readonly ICollection<string> _unknownTokens;
    private readonly StringBuilder _pending = new();
    private readonly SortedDictionary<int, PendingToolCall> _toolCalls = new();

    public PseudonymStreamRestorer(PseudonymMap map, ICollection<string> unknownTokens)
    {
        _map = map;
        _unknownTokens = unknownTokens;
    }

    public IEnumerable<string> Push(string token)
    {
        if (token.StartsWith(LLMStreamingTokens.ToolCallPrefix, StringComparison.Ordinal))
        {
            BufferToolDelta(token);
            return [];
        }

        if (token == LLMStreamingTokens.ToolCallEnd)
        {
            var output = new List<string>();
            output.AddRange(ReleaseToolCalls());
            output.Add(token);
            return output;
        }

        _pending.Append(token);
        var text = _pending.ToString();
        var holdFrom = HoldbackStart(text);
        _pending.Clear().Append(text, holdFrom, text.Length - holdFrom);
        return holdFrom == 0 ? [] : [PseudonymRestorer.RestoreText(text[..holdFrom], _map, _unknownTokens)];
    }

    public IEnumerable<string> Flush()
    {
        var output = new List<string>();
        if (_pending.Length > 0)
        {
            output.Add(PseudonymRestorer.RestoreText(_pending.ToString(), _map, _unknownTokens));
            _pending.Clear();
        }

        output.AddRange(ReleaseToolCalls());
        return output;
    }

    private static int HoldbackStart(string text)
    {
        var match = PartialTokenPattern().Match(text);
        if (!match.Success || text.Length - match.Index > PseudonymizationConstants.MaxStreamHoldbackChars)
        {
            return text.Length;
        }

        return match.Index;
    }

    private void BufferToolDelta(string token)
    {
        JsonObject? delta;
        try
        {
            delta = JsonNode.Parse(token[LLMStreamingTokens.ToolCallPrefix.Length..]) as JsonObject;
        }
        catch (JsonException)
        {
            delta = null;
        }

        if (delta == null)
        {
            return;
        }

        var index = delta[IndexProperty] is JsonValue indexValue && indexValue.TryGetValue<int>(out var parsed) ? parsed : 0;
        if (!_toolCalls.TryGetValue(index, out var call))
        {
            call = new PendingToolCall(delta);
            _toolCalls[index] = call;
        }

        call.Name.Append(StringProperty(delta, NameProperty));
        call.Arguments.Append(StringProperty(delta, ArgumentsProperty));
    }

    private IEnumerable<string> ReleaseToolCalls()
    {
        var output = new List<string>(_toolCalls.Count);
        foreach (var call in _toolCalls.Values)
        {
            var delta = (JsonObject)call.Template.DeepClone();
            delta[NameProperty] = call.Name.ToString();
            delta[ArgumentsProperty] = PseudonymRestorer.RestoreJsonArguments(call.Arguments.ToString(), _map, _unknownTokens);
            output.Add(LLMStreamingTokens.ToolCallPrefix + delta.ToJsonString());
        }

        _toolCalls.Clear();
        return output;
    }

    private static string StringProperty(JsonObject delta, string name) =>
        delta[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[A-Za-z]{1,4}(?:-?\d{0,8}(?:-[A-Za-z]?\d?)?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex PartialTokenPattern();

    private sealed class PendingToolCall
    {
        public PendingToolCall(JsonObject template)
        {
            Template = template;
        }

        public JsonObject Template { get; }

        public StringBuilder Name { get; } = new();

        public StringBuilder Arguments { get; } = new();
    }
}
