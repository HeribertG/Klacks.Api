// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared tokenizer for all search services: lowercases the search string and splits it on
/// word separators, so display formats like "Name, FirstName" match the same as "Name FirstName".
/// Text in quotes ("von Oberhausen", also „…“, “…”, «…») stays one phrase token; an unclosed quote
/// runs to the end of the input. The '+' character is NOT a separator — outside quotes it is the OR
/// operator of the search dispatch (<see cref="HasAlternatives"/>, <see cref="SplitAlternatives"/>);
/// inside quotes it is literal text ("Müller+Partner").
/// </summary>
/// <param name="searchString">Raw user search input</param>
using System.Text;

namespace Klacks.Api.Domain.Services.Common;

public static class SearchStringTokenizer
{
    private const char AlternativeSeparator = '+';

    private static readonly char[] SeparatorChars = [' ', ',', '.', '-', '/', ';'];
    private static readonly char[] QuoteChars = ['"', '„', '“', '”', '«', '»'];

    public static string[] Tokenize(string searchString)
    {
        var segments = searchString.Trim().ToLower().Split(QuoteChars);
        var tokens = new List<string>();
        var isInsideQuotes = false;

        foreach (var segment in segments)
        {
            if (isInsideQuotes)
            {
                var phrase = CollapseWhitespace(segment);
                if (phrase.Length > 0)
                {
                    tokens.Add(phrase);
                }
            }
            else
            {
                tokens.AddRange(segment.Split(SeparatorChars, StringSplitOptions.RemoveEmptyEntries));
            }

            isInsideQuotes = !isInsideQuotes;
        }

        return tokens.ToArray();
    }

    public static bool HasAlternatives(string searchString)
    {
        return SplitOutsideQuotes(searchString).Count > 1;
    }

    public static string[] SplitAlternatives(string searchString)
    {
        return SplitOutsideQuotes(searchString.ToLower())
            .Select(StripQuotes)
            .ToArray();
    }

    public static string StripQuotes(string value)
    {
        return string.Concat(value.Where(character => !QuoteChars.Contains(character)));
    }

    private static List<string> SplitOutsideQuotes(string value)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var isInsideQuotes = false;

        foreach (var character in value)
        {
            if (QuoteChars.Contains(character))
            {
                isInsideQuotes = !isInsideQuotes;
            }

            if (character == AlternativeSeparator && !isInsideQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        parts.Add(current.ToString());
        return parts;
    }

    private static string CollapseWhitespace(string value)
    {
        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
