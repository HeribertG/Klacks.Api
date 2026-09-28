// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Service for communication with the Klacks Marketplace.
/// </summary>
using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface IMarketplaceClientService
{
    Task<MarketplaceSearchResultDto?> SearchPackagesAsync(string? search, int page, int pageSize);
    Task<MarketplacePackageDto?> GetPackageAsync(string code);
    Task<byte[]?> DownloadPackageAsync(string code);
    Task<bool> UploadPackageAsync(string manifestJson, string translationsJson, string? docsJson, string? countriesJson, string? statesJson, string? calendarRulesJson);
}
