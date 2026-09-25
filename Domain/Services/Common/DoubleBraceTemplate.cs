// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fills the {{name}} placeholders of a server-written sentence that also exists in the frontend catalogue
/// (the messenger proactive texts) or is written in the same style (the escalation handoff texts). One single
/// pass over the TEMPLATE: an inserted value is never scanned again, so a value that itself contains
/// "{{date}}" (an employee name, an ERP failure reason) is not expanded a second time. A placeholder without a
/// value stays standing on purpose: removing it would produce a grammatically complete sentence that
/// silently claims a fact nobody supplied, whereas a visible {{days}} tells the reader, and whoever reads the
/// log afterwards, that a value was missing. Names are ASCII letters and digits only. The pattern has no
/// overlapping alternatives and runs non-backtracking anyway, because templates can come from a language pack.
/// </summary>
/// <param name="template">Sentence with {{name}} placeholders</param>
/// <param name="values">Value per placeholder name, already formatted for display</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Common;

public static partial class DoubleBraceTemplate
{
    private const string PlaceholderPattern = @"\{\{([A-Za-z][A-Za-z0-9]*)\}\}";
    private const int NameGroup = 1;

    public static string Render(string template, IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return template;
        }

        return Placeholder().Replace(
            template,
            match => values.TryGetValue(match.Groups[NameGroup].Value, out var value) ? value : match.Value);
    }

    /// <summary>The distinct placeholder names a template contains.</summary>
    /// <param name="template">Sentence with {{name}} placeholders</param>
    public static IReadOnlySet<string> PlaceholdersOf(string template) =>
        Placeholder().Matches(template).Select(match => match.Groups[NameGroup].Value).ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(PlaceholderPattern, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex Placeholder();
}
