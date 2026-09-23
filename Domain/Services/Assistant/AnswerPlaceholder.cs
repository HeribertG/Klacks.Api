// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Owns the assistant stand-in text written into the running history for a tool-call iteration that
/// produced no prose, and recognises that text when a model echoes it back as its own answer. Providers
/// that send the history as plain text (DeepSeek) let the model read its previous iterations as that
/// stand-in, and a model copying it verbatim ended a turn with nothing but "[Executing function calls]".
/// The retired bracketed literals (LLMLoopConstants.RetiredPlaceholders) and the current neutral sentence
/// are recognised, including repeats and surrounding whitespace, so none of them can reach the user or the
/// stored conversation as an answer.
/// </summary>
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class AnswerPlaceholder
{
    private const int MaxToolListChars = 400;

    private const char ToolListTerminator = ')';

    private static readonly Regex HistoryNoteUnit = new(
        "^" + Regex.Escape(LLMLoopConstants.ToolCallHistoryNotePrefix)
            + "[^()]*"
            + Regex.Escape(LLMLoopConstants.ToolCallHistoryNoteSuffix),
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// The assistant history text for a tool-call iteration: its own prose when it has some, otherwise a
    /// neutral sentence naming the called tools.
    /// </summary>
    /// <param name="content">Prose the model produced alongside its tool calls, may be empty.</param>
    /// <param name="functionCalls">The iteration's tool calls, named in call order without duplicates.</param>
    internal static string ForToolCallTurn(string? content, IEnumerable<LLMFunctionCall> functionCalls)
    {
        var visible = Visible(content);
        if (visible.Length > 0)
        {
            return visible;
        }

        var names = functionCalls
            .Select(call => call.FunctionName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return LLMLoopConstants.ToolCallHistoryNotePrefix
            + string.Join(LLMLoopConstants.ToolCallHistoryNoteSeparator, names)
            + LLMLoopConstants.ToolCallHistoryNoteSuffix;
    }

    /// <summary>
    /// True when the text is empty, whitespace or nothing but echoed stand-in text.
    /// </summary>
    /// <param name="text">A model answer.</param>
    internal static bool IsBlankOrPlaceholder(string? text) => Visible(text).Length == 0;

    /// <summary>
    /// The part of an answer a user may see: leading echoed stand-in text removed, and empty when nothing
    /// else remains. Text without such an echo is returned unchanged.
    /// </summary>
    /// <param name="text">A model answer.</param>
    internal static string Visible(string? text)
    {
        var rest = WithoutLeadingPlaceholders(text ?? string.Empty);
        return string.IsNullOrWhiteSpace(rest) ? string.Empty : rest;
    }

    /// <summary>
    /// True while a streamed answer could still turn out to be nothing but echoed stand-in text, i.e. every
    /// character so far is whitespace, a complete stand-in or the beginning of one.
    /// </summary>
    /// <param name="text">The content streamed so far by one provider call.</param>
    internal static bool CouldBecomePlaceholder(string text)
    {
        var rest = WithoutLeadingPlaceholders(text).TrimStart();
        if (rest.Length == 0
            || LLMLoopConstants.RetiredPlaceholders.Any(
                placeholder => placeholder.StartsWith(rest, StringComparison.OrdinalIgnoreCase))
            || LLMLoopConstants.ToolCallHistoryNotePrefix.StartsWith(rest, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return rest.StartsWith(LLMLoopConstants.ToolCallHistoryNotePrefix, StringComparison.OrdinalIgnoreCase)
            && rest.IndexOf(ToolListTerminator) < 0
            && rest.Length <= LLMLoopConstants.ToolCallHistoryNotePrefix.Length + MaxToolListChars;
    }

    private static string WithoutLeadingPlaceholders(string text)
    {
        var rest = text.TrimStart();
        var stripped = false;

        while (true)
        {
            var retired = LLMLoopConstants.RetiredPlaceholders.FirstOrDefault(
                placeholder => rest.StartsWith(placeholder, StringComparison.OrdinalIgnoreCase));
            if (retired != null)
            {
                rest = rest[retired.Length..].TrimStart();
                stripped = true;
                continue;
            }

            var note = HistoryNoteUnit.Match(rest);
            if (note.Success)
            {
                rest = rest[note.Length..].TrimStart();
                stripped = true;
                continue;
            }

            return stripped ? rest : text;
        }
    }
}
