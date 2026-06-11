// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Strips markdown formatting from assistant answers before text-to-speech synthesis so the
/// voice reads the content instead of formatting characters (e.g. "###" spoken as "Raute").
/// </summary>
/// <param name="text">The raw markdown text of the assistant message</param>
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static partial class TtsTextSanitizer
{
    public static string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var result = CodeFenceRegex().Replace(text, string.Empty);
        result = ImageRegex().Replace(result, "$1");
        result = LinkRegex().Replace(result, "$1");
        result = InlineCodeRegex().Replace(result, "$1");
        result = BoldItalicRegex().Replace(result, "$2");
        result = DoubleUnderscoreRegex().Replace(result, "$1");
        result = HeadingRegex().Replace(result, string.Empty);
        result = BlockquoteRegex().Replace(result, string.Empty);
        result = HorizontalRuleRegex().Replace(result, string.Empty);
        result = ListBulletRegex().Replace(result, string.Empty);
        result = TableSeparatorRowRegex().Replace(result, string.Empty);
        result = result.Replace('|', ' ');
        result = ExcessNewlinesRegex().Replace(result, "\n\n");

        return result.Trim();
    }

    [GeneratedRegex(@"^\s*```[^\n]*$", RegexOptions.Multiline)]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex("`([^`]*)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"(\*{1,3})(.+?)\1", RegexOptions.Singleline)]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"__(.+?)__", RegexOptions.Singleline)]
    private static partial Regex DoubleUnderscoreRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s*>\s?", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*([-*_]\s?){3,}\s*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"^\s*[-*+]\s+", RegexOptions.Multiline)]
    private static partial Regex ListBulletRegex();

    [GeneratedRegex(@"^\s*\|?[\s:|-]+\|\s*$", RegexOptions.Multiline)]
    private static partial Regex TableSeparatorRowRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessNewlinesRegex();
}
