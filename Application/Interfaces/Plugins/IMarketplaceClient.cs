// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// HTTP client interface for communicating with the Klacks Marketplace to search and download plugins.
/// </summary>
/// <param name="search">Optional search text for filtering plugins</param>
/// <param name="category">Optional category filter</param>
/// <param name="name">Plugin name to download</param>
using Klacks.Api.Application.DTOs.Plugins;

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IMarketplaceClient
{
    Task<List<MarketplacePluginInfo>> SearchPluginsAsync(string? search, string? category);
    Task<byte[]> DownloadPluginAsync(string name);
}
