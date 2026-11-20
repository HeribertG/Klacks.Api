// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The short legend the model gets in the volatile system prompt: how to treat pseudonym codes and the gender or
/// organisation of each person code that occurs in the request, so grammar ("she", "he") still works. Null when no
/// pseudonym occurs, so a request without person data keeps its prompt unchanged.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static class PseudonymLegend
{
    /// <param name="usedBaseTokens">Person tokens that occur in the pseudonymised request</param>
    /// <param name="anyPseudonym">True when any pseudonym (also a pattern value) occurs</param>
    /// <param name="dictionary">The name side, for the legend texts</param>
    public static string? Build(IEnumerable<string> usedBaseTokens, bool anyPseudonym, PseudonymDictionary dictionary)
    {
        var entries = usedBaseTokens
            .Where(token => dictionary.TryGetLegend(token, out _))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        if (entries.Count == 0 && !anyPseudonym)
        {
            return null;
        }

        var builder = new StringBuilder(PseudonymizationConstants.LegendHeader);
        if (entries.Count > 0)
        {
            builder.Append(' ').Append(PseudonymizationConstants.LegendPeopleHeader);
        }

        foreach (var token in entries)
        {
            dictionary.TryGetLegend(token, out var legend);
            builder.AppendLine().Append(string.Format(
                CultureInfo.InvariantCulture, PseudonymizationConstants.LegendEntryFormat, token, legend));
        }

        return builder.ToString();
    }
}
