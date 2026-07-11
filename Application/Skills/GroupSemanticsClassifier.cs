// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure classification logic for analyze_group_semantics: decides which semantic hypotheses
/// (canton, city, qualification, contract, geo-coordinates) a group name matches. Matching is
/// exact (trimmed, case-insensitive) against the supplied reference data — a group name can
/// match several categories at once; if none match and the group carries no coordinates it
/// falls back to Other. Contains no repository or database access so it can be unit tested
/// without mocks.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class GroupSemanticsClassifier
{
    public static IReadOnlySet<GroupSemanticCategory> Classify(
        string? groupName, bool hasGeoCoordinates, GroupSemanticsReferenceData referenceData)
    {
        var categories = new HashSet<GroupSemanticCategory>();
        var name = (groupName ?? string.Empty).Trim();

        if (name.Length > 0)
        {
            if (referenceData.CantonTokens.Contains(name))
            {
                categories.Add(GroupSemanticCategory.Canton);
            }

            if (referenceData.CityNames.Contains(name))
            {
                categories.Add(GroupSemanticCategory.City);
            }

            if (referenceData.QualificationNames.Contains(name))
            {
                categories.Add(GroupSemanticCategory.Qualification);
            }

            if (referenceData.ContractNames.Contains(name))
            {
                categories.Add(GroupSemanticCategory.Contract);
            }
        }

        if (hasGeoCoordinates)
        {
            categories.Add(GroupSemanticCategory.Geo);
        }

        if (categories.Count == 0)
        {
            categories.Add(GroupSemanticCategory.Other);
        }

        return categories;
    }
}
