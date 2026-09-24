// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fills the {name} placeholders of a clarification text. One single pass over the TEMPLATE: an inserted
/// value is never scanned again, so a value that itself contains "{question}" (employee text, a sender
/// name) is not expanded a second time. A placeholder without a value is replaced by an empty string, never
/// left standing and never an exception; whether a template carries only known placeholders is the guard
/// tests' business, since the templates are constants and packs. Names are ASCII letters and digits only, so
/// a stray brace in a translation stays literal text. The pattern has no overlapping alternatives and runs
/// non-backtracking anyway.
/// </summary>
/// <param name="template">Catalogue text with {name} placeholders</param>
/// <param name="values">Value per placeholder name, already formatted for display</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Inbound;

public static partial class ClarificationTextTemplate
{
    private const string PlaceholderPattern = @"\{([A-Za-z][A-Za-z0-9]*)\}";
    private const int NameGroup = 1;

    public static string Render(string template, IReadOnlyDictionary<string, string> values) =>
        Placeholder().Replace(template, match => values.GetValueOrDefault(match.Groups[NameGroup].Value) ?? string.Empty);

    /// <summary>The distinct placeholder names a template contains.</summary>
    /// <param name="template">Catalogue text with {name} placeholders</param>
    public static IReadOnlySet<string> PlaceholdersOf(string template) =>
        Placeholder().Matches(template).Select(match => match.Groups[NameGroup].Value).ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(PlaceholderPattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex Placeholder();
}
