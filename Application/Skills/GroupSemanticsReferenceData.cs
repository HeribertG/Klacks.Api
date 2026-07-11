// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lookup sets used by GroupSemanticsClassifier to decide which category a group name matches.
/// Every set is case-insensitive and holds trimmed, non-empty values.
/// </summary>
/// <param name="CantonTokens">Canton abbreviations and multilingual canton names.</param>
/// <param name="CityNames">Distinct client home-address cities.</param>
/// <param name="QualificationNames">Multilingual qualification master names.</param>
/// <param name="ContractNames">Names of non-deleted contracts.</param>

namespace Klacks.Api.Application.Skills;

internal sealed record GroupSemanticsReferenceData(
    IReadOnlySet<string> CantonTokens,
    IReadOnlySet<string> CityNames,
    IReadOnlySet<string> QualificationNames,
    IReadOnlySet<string> ContractNames);
