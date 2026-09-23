// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders executed tool calls into the delimited tool-result block that is fed back into an LLM loop as a
/// "user" message. Shared by the chat loop, the turn replay and the read-only research sub-loop so every
/// path applies the same prompt-injection containment:
/// (1) every result gets its own [Result: name] … [/Result] frame, so a newline inside a result cannot
///     forge a sibling entry;
/// (2) the skill name and the result body are escaped against all four delimiters, so content cannot close
///     its own frame or open a new one — this covers trusted skills too, because imported and user-entered
///     strings flow through ordinary read skills;
/// (3) results carrying content authored outside this system (listed in UntrustedSkillOutputs or tainted
///     by a relaying wrapper skill) are flagged untrusted and carry an explicit data-not-instructions
///     notice, matched by the UNTRUSTED TOOL CONTENT rule of the system prompt.
/// </summary>
/// <param name="entries">Executed tool calls in the order the model issued them.</param>
/// <param name="maxToolResultChars">Per-result character cap applied after escaping.</param>

using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ToolResultFormatter
{
    public static string Format(IEnumerable<LLMFunctionCall> calls, int maxToolResultChars) =>
        Format(
            calls.Select(call => new ToolResultEntry(call.FunctionName, call.Result, call.ContainsExternalContent)),
            maxToolResultChars);

    public static string Format(IEnumerable<ToolResultEntry> entries, int maxToolResultChars)
    {
        var sb = new StringBuilder();
        sb.AppendLine(ToolResultMarkers.BlockHeader);
        foreach (var entry in entries)
        {
            var isUntrusted = IsUntrusted(entry);

            // Escape BEFORE capping: escaping replaces a 9-character delimiter with a 16-character
            // placeholder, so capping first would let a result built from repeated forged delimiters
            // grow ~1.8x past the cap — attacker-controlled history inflation, which is the very thing
            // the cap exists to prevent.
            var body = entry.Result is null
                ? ToolResultMarkers.EmptyResultPlaceholder
                : EscapeAndCap(entry.Result, maxToolResultChars) ?? ToolResultMarkers.EmptyResultPlaceholder;

            sb.Append(ToolResultMarkers.ResultOpenPrefix);
            sb.Append(ToolResultSanitizer.EscapeDelimiters(entry.Name));
            if (isUntrusted)
            {
                sb.Append(ToolResultMarkers.ResultUntrustedFlag);
            }

            sb.AppendLine(ToolResultMarkers.ResultOpenSuffix);

            if (isUntrusted)
            {
                sb.AppendLine(ToolResultMarkers.UntrustedContentNotice);
            }

            sb.AppendLine(body);
            sb.AppendLine(ToolResultMarkers.ResultClose);
        }

        sb.AppendLine(ToolResultMarkers.BlockFooter);
        return sb.ToString();
    }

    /// <summary>
    /// Escapes the delimiters of a result body and caps it, in that order (see Format). Shared with the MCP
    /// handler so an untrusted result is contained the same way on every channel.
    /// </summary>
    /// <param name="result">The raw result text.</param>
    /// <param name="maxToolResultChars">Character cap applied after escaping.</param>
    public static string? EscapeAndCap(string? result, int maxToolResultChars) =>
        result is null ? null : CapToolResult(ToolResultSanitizer.EscapeDelimiters(result), maxToolResultChars);

    public static bool IsUntrusted(ToolResultEntry entry) =>
        entry.ContainsExternalContent || UntrustedSkillOutputs.Contains(entry.Name);

    // A single skill can return a large payload (e.g. a long list). Fed back verbatim into the loop this
    // would inflate the running history until the prompt exceeds the model's input limit. Cap it so the
    // model still sees the head of the result plus an explicit truncation marker.
    private static string? CapToolResult(string? result, int maxToolResultChars)
    {
        if (string.IsNullOrEmpty(result) || result.Length <= maxToolResultChars)
            return result;

        return result[..maxToolResultChars]
            + $"\n[Result truncated: {result.Length} chars total, showing first {maxToolResultChars}.]";
    }
}
