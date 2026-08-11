// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared tokenizer for all search services: lowercases the search string and splits it on
/// word separators, so display formats like "Name, FirstName" match the same as "Name FirstName".
/// The '+' character is NOT a separator — it is reserved as the OR operator of the search dispatch.
/// </summary>
/// <param name="searchString">Raw user search input</param>
namespace Klacks.Api.Domain.Services.Common;

public static class SearchStringTokenizer
{
    private static readonly char[] SeparatorChars = [' ', ',', '.', '-', '/', ';'];

    public static string[] Tokenize(string searchString)
    {
        return searchString.Trim().ToLower()
            .Split(SeparatorChars, StringSplitOptions.RemoveEmptyEntries);
    }
}
