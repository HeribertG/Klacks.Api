// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wraps text that originates from an employee or from an earlier model step in an untrusted-data tag
/// pair for an LLM prompt. Any closing tag of that pair inside the text is neutralized to a bracketed form,
/// so the text can never break out of its block or be mistaken for a system fact. Besides the exact tag,
/// the neutralization catches case variants, whitespace inside the tag, invisible format characters
/// (zero-width space, joiners, BOM, soft hyphen), fullwidth or look-alike angle brackets and slashes,
/// fullwidth tag-name letters and trailing junk before the closing bracket. Ordinary text such as
/// "shift &lt;14:00" is left untouched. The pattern is built per tag and cached; it uses the linear-time
/// NonBacktracking engine, so attacker-controlled text cannot make the match hang.
/// </summary>
/// <param name="text">The untrusted text</param>
/// <param name="openTag">Opening tag, for example "&lt;employee_message&gt;"</param>
/// <param name="closeTag">Matching closing tag, for example "&lt;/employee_message&gt;"</param>

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Klacks.Api.Infrastructure.Inbound;

internal static class UntrustedTextBlock
{
    private const string NeutralizedTagOpenBracket = "[";
    private const string NeutralizedTagCloseBracket = "]";
    private const string IgnorableRun = @"[\s\p{Cf}]*";
    private const string FormatRun = @"\p{Cf}*";
    private const string TagOpenChars = "[<＜﹤‹〈]";
    private const string TagCloseChars = "[>＞﹥›〉]";
    private const string SlashChars = "[/\\⁄∕／＼]";
    private const string TrailingJunk = "[^<>＜＞﹤﹥‹›〈〉]{0,64}?";
    private const int FullwidthOffset = 0xFEE0;
    private const char FullwidthLowLine = '＿';
    private const char LowLine = '_';
    private const int ClosingTagNameStart = 2;

    private static readonly ConcurrentDictionary<string, Regex> ClosingTagPatterns = new(StringComparer.Ordinal);

    internal static string Wrap(string text, string openTag, string closeTag) =>
        openTag + NeutralizeClosingTag(text, closeTag) + closeTag;

    internal static string NeutralizeClosingTag(string text, string closeTag)
    {
        var pattern = ClosingTagPatterns.GetOrAdd(closeTag, BuildPattern);
        var replacement = NeutralizedTagOpenBracket + closeTag[1..^1] + NeutralizedTagCloseBracket;

        return pattern.Replace(text, replacement);
    }

    private static Regex BuildPattern(string closeTag)
    {
        var name = closeTag[ClosingTagNameStart..^1];
        var pattern = new StringBuilder()
            .Append(TagOpenChars).Append(IgnorableRun)
            .Append(SlashChars).Append(IgnorableRun);

        foreach (var character in name)
        {
            pattern.Append(FormatRun).Append(CharacterAlternatives(character));
        }

        pattern.Append("(?:").Append(TrailingJunk).Append(TagCloseChars).Append(")?");

        return new Regex(
            pattern.ToString(),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    }

    private static string CharacterAlternatives(char character)
    {
        if (character == LowLine)
        {
            return "[" + LowLine + FullwidthLowLine + "]";
        }

        if (char.IsAsciiLetterOrDigit(character))
        {
            var fullwidth = (char)(character + FullwidthOffset);
            return "[" + character + fullwidth + "]";
        }

        return Regex.Escape(character.ToString());
    }
}
