// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the Strict name policy (NameResolution) to client search rows. A client may be called by its first and
/// last name in either order ("Hans Meier" and "Meier Hans"), by either name alone, by its company (customers often
/// have nothing else), and any of these may carry the visible client number ("Meier 1042"). Equal words and a
/// forward match on whole words are matches; a partial word ("Mei" for "Meier"), a nickname, a diacritic the stored
/// name does not have, or a typo or phonetic similarity ("Meier" for "Mayer") only makes the row a suggestion.
/// </summary>
/// <param name="items">Rows of the client text search or of its fuzzy fallback</param>
/// <param name="term">The name as the user said it</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Skills;

internal static class ClientNameMatch
{
    public static NamePartition<ClientSearchItem> Partition(IReadOnlyList<ClientSearchItem> items, string? term)
    {
        return NameResolution.PartitionVariants(items, Variants, term);
    }

    private static IEnumerable<string?> Variants(ClientSearchItem item)
    {
        var firstLast = $"{item.FirstName} {item.LastName}";
        var lastFirst = $"{item.LastName} {item.FirstName}";
        yield return firstLast;
        yield return lastFirst;
        yield return item.Company;

        if (item.IdNumber is { } idNumber)
        {
            var number = idNumber.ToString(CultureInfo.InvariantCulture);
            yield return $"{firstLast} {number}";
            yield return $"{lastFirst} {number}";
            yield return $"{item.Company} {number}";
        }
    }
}
