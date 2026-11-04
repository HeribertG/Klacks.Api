// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs a feature plugin from the Klacks Marketplace: size-capped download, vendor signature check,
/// zip-bomb check, extraction into the plugin directory and plugin installation.
/// </summary>
/// <param name="name">Plugin name as listed in the marketplace; also the target directory name</param>
using Klacks.Api.Application.DTOs.Plugins;

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IMarketplacePluginInstaller
{
    Task<MarketplacePluginInstallStatus> InstallAsync(string name, CancellationToken cancellationToken);
}
