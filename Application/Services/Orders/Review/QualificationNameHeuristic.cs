// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Suggests the qualifications an order should require by name: a qualification is suggested when one of its names
/// (any language) occurs at a word start in the order's name or description or in the name of the group that staffs
/// it ("Nachtwache Pflegefachperson", group "Pflegefachpersonen Bern"). Groups formed after a qualification are only
/// recognised by their name, there is no flag on the group (owner decision 2026-10-23). Names shorter than
/// MinNameLength and qualifications the order already requires are left out. Pure.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class QualificationNameHeuristic
{
    public const int MinNameLength = 4;

    public static List<Qualification> Suggest(
        IEnumerable<string?> texts, IReadOnlyList<Qualification> qualifications, IReadOnlyCollection<Guid> alreadyRequired)
    {
        var haystacks = texts.Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text!).ToList();
        if (haystacks.Count == 0)
        {
            return [];
        }

        return qualifications
            .Where(qualification => !qualification.IsDeleted && !alreadyRequired.Contains(qualification.Id))
            .Where(qualification => NamesOf(qualification).Any(name => haystacks.Any(text => OccursAtWordStart(text, name))))
            .ToList();
    }

    private static IEnumerable<string> NamesOf(Qualification qualification) =>
        qualification.Name.GetAllValues().Values
            .Where(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= MinNameLength)
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static bool OccursAtWordStart(string text, string name)
    {
        var index = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            if (index == 0 || !char.IsLetterOrDigit(text[index - 1]))
            {
                return true;
            }

            index = text.IndexOf(name, index + 1, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
