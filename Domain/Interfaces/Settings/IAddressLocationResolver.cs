// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Geo;

namespace Klacks.Api.Domain.Interfaces.Settings;

/// <summary>
/// Resolves location filters as users say them (a country or state by code or by its name in any installed
/// language) to the codes stored on addresses, using the states and countries tables.
/// </summary>
public interface IAddressLocationResolver
{
    /// <summary>
    /// Builds the criteria for the order location filter: the country to its code, the state to every matching
    /// (code, country) pair, limited to the country when one is given; city and zip prefix are only trimmed.
    /// Unknown text is kept as typed.
    /// </summary>
    /// <param name="country">Country code or name; null/blank = any country</param>
    /// <param name="state">State code or name (canton, Bundesland, ...); null/blank = any state</param>
    /// <param name="city">City; null/blank = any city</param>
    /// <param name="zipPrefix">Zip code prefix; null/blank = any zip code</param>
    Task<AddressLocationCriteria> ResolveAsync(
        string? country, string? state, string? city, string? zipPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// The state code for a code or name of any country (code matches first, then names); unknown text is returned
    /// trimmed as typed, blank input as null. For the client search, whose filter is a bare state code.
    /// </summary>
    /// <param name="state">State code or name</param>
    Task<string?> ResolveStateCodeAsync(string? state, CancellationToken cancellationToken = default);
}
