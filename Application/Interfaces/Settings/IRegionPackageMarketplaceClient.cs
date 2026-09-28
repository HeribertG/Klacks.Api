// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    Task<MarketplaceRegionPackageDownload?> DownloadProfileAsync(string countryCode, CancellationToken cancellationToken);
}
