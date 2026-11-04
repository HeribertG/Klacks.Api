// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// REST API controller for feature plugin management: listing, installation, enable/disable. Listing
/// requires an authenticated user (the navigation of every signed-in user reads it); all changes are
/// admin-only.
/// </summary>
/// <param name="featurePluginService">Service handling feature plugin lifecycle operations</param>
/// <param name="marketplaceClient">Marketplace search client; null when no marketplace is configured</param>
/// <param name="marketplacePluginInstaller">Verified marketplace installation; null when no marketplace is configured</param>

using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Plugins;

[ApiController]
[Route("api/plugins/features")]
public class FeaturePluginController : ControllerBase
{
    private const string MarketplaceNotConfiguredMessage = "Marketplace integration not configured";

    private readonly IFeaturePluginService _featurePluginService;
    private readonly IMarketplaceClient? _marketplaceClient;
    private readonly IMarketplacePluginInstaller? _marketplacePluginInstaller;

    public FeaturePluginController(
        IFeaturePluginService featurePluginService,
        IMarketplaceClient? marketplaceClient = null,
        IMarketplacePluginInstaller? marketplacePluginInstaller = null)
    {
        _featurePluginService = featurePluginService;
        _marketplaceClient = marketplaceClient;
        _marketplacePluginInstaller = marketplacePluginInstaller;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeaturePluginInfo>>> GetAllPlugins()
    {
        return Ok(await _featurePluginService.GetAllPluginsAsync());
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("{name}")]
    public async Task<ActionResult<FeaturePluginInfo>> GetPlugin(string name)
    {
        var plugin = await _featurePluginService.GetPluginAsync(name);
        if (plugin == null)
            return NotFound();

        return Ok(plugin);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [BlockedOnPlayground]
    [HttpPost("{name}/install")]
    public async Task<ActionResult> InstallPlugin(string name)
    {
        var result = await _featurePluginService.InstallAsync(name);
        if (!result)
            return BadRequest("Feature plugin not found or version incompatible");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [BlockedOnPlayground]
    [HttpDelete("{name}/uninstall")]
    public async Task<ActionResult> UninstallPlugin(string name)
    {
        var result = await _featurePluginService.UninstallAsync(name);
        if (!result)
            return BadRequest("Feature plugin not found");

        return Ok();
    }

    [BlockedOnPlayground]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("{name}/enable")]
    public async Task<ActionResult> EnablePlugin(string name)
    {
        var result = await _featurePluginService.EnableAsync(name);
        if (!result)
            return BadRequest("Feature plugin not installed");

        return Ok();
    }

    [BlockedOnPlayground]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("{name}/disable")]
    public async Task<ActionResult> DisablePlugin(string name)
    {
        var result = await _featurePluginService.DisableAsync(name);
        if (!result)
            return BadRequest("Feature plugin not installed");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpGet("marketplace")]
    public async Task<ActionResult<List<MarketplacePluginInfo>>> SearchMarketplace(
        [FromQuery] string? search,
        [FromQuery] string? category)
    {
        if (_marketplaceClient is null)
            return BadRequest(MarketplaceNotConfiguredMessage);

        var plugins = await _marketplaceClient.SearchPluginsAsync(search, category);
        return Ok(plugins);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [BlockedOnPlayground]
    [HttpPost("{name}/install-from-marketplace")]
    public async Task<ActionResult> InstallFromMarketplace(string name, CancellationToken cancellationToken)
    {
        if (_marketplacePluginInstaller is null)
            return BadRequest(MarketplaceNotConfiguredMessage);

        var status = await _marketplacePluginInstaller.InstallAsync(name, cancellationToken);

        return status switch
        {
            MarketplacePluginInstallStatus.Installed => Ok(new { message = $"Plugin '{name}' installed from marketplace" }),
            MarketplacePluginInstallStatus.InvalidName => BadRequest("Invalid plugin name"),
            MarketplacePluginInstallStatus.Unavailable => BadRequest("Failed to download plugin from marketplace"),
            MarketplacePluginInstallStatus.TooLarge => BadRequest("Plugin package exceeds the size limit"),
            MarketplacePluginInstallStatus.SignatureRejected => BadRequest("Plugin package signature could not be verified"),
            MarketplacePluginInstallStatus.InvalidArchive => BadRequest("Plugin package is not a valid archive or exceeds the archive limits"),
            MarketplacePluginInstallStatus.ManifestMismatch => BadRequest("Plugin package manifest does not match the requested plugin"),
            _ => BadRequest("Plugin downloaded but installation failed — version may be incompatible")
        };
    }
}
