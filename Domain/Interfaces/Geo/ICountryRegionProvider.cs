// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Geo;

public interface ICountryRegionProvider
{
    Task<IReadOnlyDictionary<string, string>> GetRegionByStateAsync(string countryCode, CancellationToken cancellationToken = default);
}
