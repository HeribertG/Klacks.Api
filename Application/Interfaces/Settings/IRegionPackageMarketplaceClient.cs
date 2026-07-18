// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Client for the region-package endpoints of the Klacks Marketplace. GetLatestAsync reports a
/// country without a published package (HTTP 404) via the lookup result's NotFound flag instead of
/// treating it as a failure.
/// </summary>
using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IRegionPackageMarketplaceClient
{
    Task<MarketplaceRegionPackageLookup> GetLatestAsync(string countryCode, CancellationToken cancellationToken);

    Task<string?> DownloadProfileJsonAsync(string countryCode, CancellationToken cancellationToken);
}
