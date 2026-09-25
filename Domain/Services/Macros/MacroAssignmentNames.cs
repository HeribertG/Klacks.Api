// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Makes a free-text name (shift, absence type or macro name, possibly written by the assistant itself) safe to embed in a
/// server-authored text the language model relays: every whitespace character becomes a space and runs collapse; control,
/// format (including the invisible tag and direction-override characters), private-use and unassigned characters and lone
/// surrogate halves are removed, while emoji and other astral characters stay whole; the quote <see cref="Delimiter"/> that
/// encloses every name in those texts is replaced by a look-alike, so a name can never close its own quotes; and the
/// result is cut to a fixed length (never inside a surrogate pair). A crafted name can therefore neither fake extra lines,
/// hide instructions, step out of its quotes nor push the actual facts out of a length-capped tool result. The same
/// cleaning with the longer <see cref="MaxDetailLength"/> serves detail texts such as the error of a macro that cannot run.
/// </summary>
/// <param name="name">The raw name as stored</param>
/// <param name="text">A raw detail text</param>
/// <param name="maxLength">The length the result is cut to, ellipsis included</param>

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Macros;

public static class MacroAssignmentNames
{
    public const int MaxLength = 60;
    public const int MaxDetailLength = 200;
    public const char Delimiter = '\'';
    public const char DelimiterReplacement = '’';

    private const string Ellipsis = "...";
    private const char Space = ' ';
    private const string SpaceRunPattern = " {2,}";

    private static readonly Regex SpaceRuns = new(SpaceRunPattern, RegexOptions.Compiled);

    private static readonly IReadOnlySet<UnicodeCategory> RemovedCategories = new HashSet<UnicodeCategory>
    {
        UnicodeCategory.Control,
        UnicodeCategory.Format,
        UnicodeCategory.PrivateUse,
        UnicodeCategory.OtherNotAssigned
    };

    public static string Safe(string? name) => Safe(name, MaxLength);

    public static string Safe(string? text, int maxLength)
    {
        var flat = SpaceRuns.Replace(Clean(text ?? string.Empty), Space.ToString()).Trim();
        return flat.Length <= maxLength ? flat : Cut(flat, maxLength - Ellipsis.Length) + Ellipsis;
    }

    private static string Clean(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (char.IsSurrogatePair(text, index))
            {
                if (!RemovedCategories.Contains(CharUnicodeInfo.GetUnicodeCategory(char.ConvertToUtf32(text, index))))
                {
                    builder.Append(current).Append(text[index + 1]);
                }

                index++;
            }
            else if (char.IsWhiteSpace(current))
            {
                builder.Append(Space);
            }
            else if (!char.IsSurrogate(current) && !RemovedCategories.Contains(char.GetUnicodeCategory(current)))
            {
                builder.Append(current == Delimiter ? DelimiterReplacement : current);
            }
        }

        return builder.ToString();
    }

    private static string Cut(string text, int length) =>
        char.IsHighSurrogate(text[length - 1]) ? text[..(length - 1)] : text[..length];
}
