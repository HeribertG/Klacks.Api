// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replaces internal technical entity names with their user-facing terms in curated knowledge
/// markdown before it is served to the assistant, as a code-side safety net behind the prompt
/// rule. Replacements are applied to prose only: text inside inline code spans (backticks) and
/// fenced code blocks is left verbatim, so DOM element ids, skill names and intentional
/// internal-to-business translation tables stay intact.
/// </summary>
/// <param name="content">The raw knowledge markdown to sanitize; returned unchanged when it carries no internal names</param>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class KnowledgeContentSanitizer
{
    private const char InlineCodeDelimiter = '`';
    private const string FenceMarker = "```";

    public static string Sanitize(string? content)
    {
        if (string.IsNullOrEmpty(content) || !InternalEntityNames.ContainsAny(content))
        {
            return content ?? string.Empty;
        }

        var lines = content.Split('\n');
        var insideFence = false;
        var changed = false;

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith(FenceMarker, StringComparison.Ordinal))
            {
                insideFence = !insideFence;
                continue;
            }

            if (insideFence)
            {
                continue;
            }

            var sanitizedLine = SanitizeProseLine(lines[i]);
            if (!ReferenceEquals(sanitizedLine, lines[i]))
            {
                lines[i] = sanitizedLine;
                changed = true;
            }
        }

        return changed ? string.Join('\n', lines) : content;
    }

    private static string SanitizeProseLine(string line)
    {
        if (!InternalEntityNames.ContainsAny(line))
        {
            return line;
        }

        var segments = line.Split(InlineCodeDelimiter);

        // An odd backtick count leaves no valid inline code span on the line (CommonMark),
        // so treat the whole line as prose — otherwise a stray backtick could hide a name.
        if (segments.Length % 2 == 0)
        {
            return ReplaceInternalNames(line);
        }

        var changed = false;

        for (var i = 0; i < segments.Length; i += 2)
        {
            var replaced = ReplaceInternalNames(segments[i]);
            if (!ReferenceEquals(replaced, segments[i]))
            {
                segments[i] = replaced;
                changed = true;
            }
        }

        return changed ? string.Join(InlineCodeDelimiter, segments) : line;
    }

    private static string ReplaceInternalNames(string segment)
    {
        if (!InternalEntityNames.ContainsAny(segment))
        {
            return segment;
        }

        var result = segment;
        foreach (var (name, userTerm) in InternalEntityNames.Replacements)
        {
            result = result.Replace(name, userTerm, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
