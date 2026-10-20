// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Geo;

namespace Klacks.Api.Domain.Interfaces.Geo;

public interface ICountryRegionProvider
{
    /// <summary>
    /// Region of every state of the country, keyed by upper-case state code (case-insensitive lookup).
    /// A country without a region file yields an empty map; alpha-3 country codes are accepted.
    /// </summary>
    Task<IReadOnlyDictionary<string, CountryRegion>> GetRegionByStateAsync(string countryCode, CancellationToken cancellationToken = default);
}
