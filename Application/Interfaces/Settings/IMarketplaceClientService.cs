// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Service for communication with the Klacks Marketplace. DownloadPackageAsync returns the language pack
/// bundle only after a size-capped download whose vendor signature was accepted.
/// </summary>
using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IMarketplaceClientService
{
    Task<MarketplaceSearchResultDto?> SearchPackagesAsync(string? search, int page, int pageSize);
    Task<MarketplacePackageDto?> GetPackageAsync(string code);
    Task<MarketplacePackageDownload> DownloadPackageAsync(string code, CancellationToken cancellationToken);
    Task<bool> UploadPackageAsync(string manifestJson, string translationsJson, string? docsJson, string? countriesJson, string? statesJson, string? calendarRulesJson);
}
