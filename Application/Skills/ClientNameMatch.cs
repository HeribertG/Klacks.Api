// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the Strict name policy (NameResolution) to client search rows, row by row. A client may be called by its
/// first and last name in either order ("Hans Meier" and "Meier Hans"), by either name alone, by its company when it
/// is a customer or has no person name (an employee "Anna Keller" of "Meier Transport AG" is not called "Meier"), and
/// any of these may carry the visible client number ("Meier 1042"). A row
/// is a match when one of these names has equal words, a forward match on whole words, an unsegmented-script
/// substring or a label-stripped compact match with the term; a partial word ("Mei" for "Meier"), a nickname, a
/// diacritic the stored name does not have, or a typo or phonetic similarity ("Meier" for "Mayer") only makes the
/// row a suggestion. Rows are judged one by one rather than stage-first, so an exact company name "Meier" does not
/// push the person "Hans Meier" out of the matches; ClientResolver then asks which one is meant.
/// </summary>
/// <param name="items">Rows of the client text search or of its fuzzy search</param>
/// <param name="term">The name as the user said it</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class ClientNameMatch
{
    public static NamePartition<ClientSearchItem> Partition(IReadOnlyList<ClientSearchItem> items, string? term)
    {
        var matches = new List<ClientSearchItem>();
        var suggestions = new List<ClientSearchItem>();
        foreach (var item in items)
        {
            var named = NameResolution.PartitionVariants([item], Variants, term).Matches.Count > 0;
            (named ? matches : suggestions).Add(item);
        }

        return new NamePartition<ClientSearchItem>(matches, suggestions);
    }

    private static IEnumerable<string?> Variants(ClientSearchItem item)
    {
        var firstLast = $"{item.FirstName} {item.LastName}";
        var lastFirst = $"{item.LastName} {item.FirstName}";
        var company = CalledByCompany(item) ? item.Company : null;
        yield return firstLast;
        yield return lastFirst;
        yield return company;

        if (item.IdNumber is { } idNumber)
        {
            var number = idNumber.ToString(CultureInfo.InvariantCulture);
            yield return $"{firstLast} {number}";
            yield return $"{lastFirst} {number}";
            if (company != null)
            {
                yield return $"{company} {number}";
            }
        }
    }

    private static bool CalledByCompany(ClientSearchItem item)
    {
        return string.Equals(item.EntityType, nameof(EntityTypeEnum.Customer), StringComparison.Ordinal)
               || (string.IsNullOrWhiteSpace(item.FirstName) && string.IsNullOrWhiteSpace(item.LastName));
    }
}
