// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Geo;

public interface ICountryRegionProvider
{
    Task<IReadOnlyDictionary<string, string>> GetRegionByStateAsync(string countryCode, CancellationToken cancellationToken = default);
}
