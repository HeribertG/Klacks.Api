// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Geo;

/// <summary>
/// Resolved location filter on one address: country, state, city and zip prefix, every set part must hold on the
/// same address. Names are already resolved to the codes stored on addresses (IAddressLocationResolver); the
/// predicate is AddressLocationPredicate.
/// </summary>
/// <param name="CountryCode">Country code the address must carry (e.g. 'CH'); null = any country</param>
/// <param name="States">States the address may lie in (any of them); empty = any state</param>
/// <param name="City">City the address must carry, compared case-insensitively; null = any city</param>
/// <param name="ZipPrefix">Prefix the address zip code must start with; null = any zip code</param>
/// <param name="CountryCodes">Countries the address may lie in (any of them, multi-select); null or empty = any country. Must hold in addition to CountryCode</param>
public sealed record AddressLocationCriteria(
    string? CountryCode,
    IReadOnlyList<StateCodeMatch> States,
    string? City,
    string? ZipPrefix,
    IReadOnlyList<string>? CountryCodes = null)
{
    public static AddressLocationCriteria None { get; } = new(null, [], null, null);

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(CountryCode)
        && (CountryCodes == null || CountryCodes.Count == 0)
        && States.Count == 0
        && string.IsNullOrWhiteSpace(City)
        && string.IsNullOrWhiteSpace(ZipPrefix);

    /// <summary>
    /// The client-search filters as they reach the repository: a state code of any country, a city and a zip prefix.
    /// </summary>
    public static AddressLocationCriteria ForStateCode(string? stateCode, string? city, string? zipPrefix)
    {
        IReadOnlyList<StateCodeMatch> states = string.IsNullOrWhiteSpace(stateCode)
            ? []
            : [new StateCodeMatch(stateCode.Trim(), null)];

        return new AddressLocationCriteria(null, states, Clean(city), Clean(zipPrefix));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
