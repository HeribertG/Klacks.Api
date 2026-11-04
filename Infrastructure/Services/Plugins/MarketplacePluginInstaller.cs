// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs a feature plugin from the Klacks Marketplace. Order: validate the name and resolve the target
/// directory under Plugins/Features, download with byte cap and vendor signature check
/// (<see cref="IMarketplaceClient"/>), run the zip-bomb check (entries, unpacked bytes, compression
/// ratio), require the archive manifest to declare exactly the requested name, and only then replace the
/// plugin directory (<see cref="PluginDirectoryReplacer"/>: staged extraction next to the plugin root, then
/// swap) and install. Nothing is written to disk before the signature, the zip check and the manifest
/// check passed; a failed extraction leaves the previous plugin directory in place.
/// </summary>
/// <param name="marketplaceClient">Marketplace client returning only size-capped, signature-verified bytes</param>
/// <param name="featurePluginService">Plugin registry refreshed and asked to install after extraction</param>
/// <param name="logger">Logger for diagnostic output</param>
/// <param name="pluginRootDirectory">Directory that holds one sub-directory per feature plugin; its parent hosts the staging directories</param>
using System.Text.RegularExpressions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Services.Archives;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public class MarketplacePluginInstaller : IMarketplacePluginInstaller
{
    private const string PluginNamePattern = @"^[A-Za-z0-9_-]+\z";

    private static readonly Regex PluginNameRegex = new(PluginNamePattern, RegexOptions.Compiled);

    private static readonly ZipArchiveLimits PluginArchiveLimits = new(
        MarketplacePackageLimits.MaxPluginUnpackedBytes,
        MarketplacePackageLimits.MaxPluginZipEntries,
        MarketplacePackageLimits.MaxPluginCompressionRatio,
        MarketplacePackageLimits.CompressionRatioCheckThresholdBytes);

    private readonly IMarketplaceClient _marketplaceClient;
    private readonly IFeaturePluginService _featurePluginService;
    private readonly ILogger<MarketplacePluginInstaller> _logger;
    private readonly string _pluginRootDirectory;
    private readonly string _workDirectory;

    public MarketplacePluginInstaller(
        IMarketplaceClient marketplaceClient,
        IFeaturePluginService featurePluginService,
        ILogger<MarketplacePluginInstaller> logger)
        : this(
            marketplaceClient,
            featurePluginService,
            logger,
            Path.Combine(AppContext.BaseDirectory, FeaturePluginConstants.PluginDirectory))
    {
    }

    internal MarketplacePluginInstaller(
        IMarketplaceClient marketplaceClient,
        IFeaturePluginService featurePluginService,
        ILogger<MarketplacePluginInstaller> logger,
        string pluginRootDirectory)
    {
        _marketplaceClient = marketplaceClient;
        _featurePluginService = featurePluginService;
        _logger = logger;
        _pluginRootDirectory = Path.GetFullPath(pluginRootDirectory);
        _workDirectory = Path.GetDirectoryName(_pluginRootDirectory.TrimEnd(Path.DirectorySeparatorChar))!;
    }

    public async Task<MarketplacePluginInstallStatus> InstallAsync(string name, CancellationToken cancellationToken)
    {
        if (!TryResolvePluginDir(name, out var pluginDir))
        {
            return MarketplacePluginInstallStatus.InvalidName;
        }

        var download = await _marketplaceClient.DownloadPluginAsync(name, cancellationToken);
        if (!download.Succeeded)
        {
            return download.Status switch
            {
                MarketplaceDownloadStatus.TooLarge => MarketplacePluginInstallStatus.TooLarge,
                MarketplaceDownloadStatus.SignatureRejected => MarketplacePluginInstallStatus.SignatureRejected,
                _ => MarketplacePluginInstallStatus.Unavailable
            };
        }

        var check = ZipArchiveLimitGuard.Check(download.Content, PluginArchiveLimits);
        if (!check.IsWithinLimits)
        {
            _logger.LogWarning(
                check.Cause,
                "Marketplace plugin '{Plugin}' rejected by the archive check: {Reason}",
                name.ForLog(),
                check.Message);
            return MarketplacePluginInstallStatus.InvalidArchive;
        }

        if (!PluginArchiveManifestCheck.DeclaresPlugin(download.Content, name))
        {
            _logger.LogWarning(
                "Marketplace plugin '{Plugin}' rejected: the archive manifest does not declare this plugin name",
                name.ForLog());
            return MarketplacePluginInstallStatus.ManifestMismatch;
        }

        if (!TryReplacePluginDirectory(name, download.Content, pluginDir))
        {
            return MarketplacePluginInstallStatus.InvalidArchive;
        }

        await _featurePluginService.RefreshPluginsAsync();

        return await _featurePluginService.InstallAsync(name)
            ? MarketplacePluginInstallStatus.Installed
            : MarketplacePluginInstallStatus.InstallFailed;
    }

    private bool TryReplacePluginDirectory(string name, byte[] content, string pluginDir)
    {
        try
        {
            PluginDirectoryReplacer.Replace(content, pluginDir, _workDirectory, _logger);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException
                                       or ArgumentException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Marketplace plugin '{Plugin}' could not be extracted", name.ForLog());
            return false;
        }
    }

    private bool TryResolvePluginDir(string name, out string pluginDir)
    {
        pluginDir = string.Empty;

        if (string.IsNullOrWhiteSpace(name) || !PluginNameRegex.IsMatch(name))
        {
            return false;
        }

        var baseWithSeparator = _pluginRootDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _pluginRootDirectory
            : _pluginRootDirectory + Path.DirectorySeparatorChar;

        var candidate = Path.GetFullPath(Path.Combine(_pluginRootDirectory, name));
        if (!candidate.StartsWith(baseWithSeparator, StringComparison.Ordinal))
        {
            return false;
        }

        pluginDir = candidate;
        return true;
    }
}
