// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API controller for feature plugin management: listing, installation, enable/disable.
/// </summary>
/// <param name="featurePluginService">Service handling feature plugin lifecycle operations</param>

using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Plugins;

[ApiController]
[Route("api/plugins/features")]
public class FeaturePluginController : ControllerBase
{
    private readonly IFeaturePluginService _featurePluginService;

    public FeaturePluginController(IFeaturePluginService featurePluginService)
    {
        _featurePluginService = featurePluginService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<FeaturePluginInfo>> GetAllPlugins()
    {
        return Ok(_featurePluginService.GetAllPlugins());
    }

    [HttpGet("{name}")]
    public ActionResult<FeaturePluginInfo> GetPlugin(string name)
    {
        var plugin = _featurePluginService.GetPlugin(name);
        if (plugin == null)
            return NotFound();

        return Ok(plugin);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("{name}/install")]
    public async Task<ActionResult> InstallPlugin(string name)
    {
        var result = await _featurePluginService.InstallAsync(name);
        if (!result)
            return BadRequest("Feature plugin not found or version incompatible");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpDelete("{name}/uninstall")]
    public async Task<ActionResult> UninstallPlugin(string name)
    {
        var result = await _featurePluginService.UninstallAsync(name);
        if (!result)
            return BadRequest("Feature plugin not found");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("{name}/enable")]
    public async Task<ActionResult> EnablePlugin(string name)
    {
        var result = await _featurePluginService.EnableAsync(name);
        if (!result)
            return BadRequest("Feature plugin not installed");

        return Ok();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("{name}/disable")]
    public async Task<ActionResult> DisablePlugin(string name)
    {
        var result = await _featurePluginService.DisableAsync(name);
        if (!result)
            return BadRequest("Feature plugin not installed");

        return Ok();
    }
}
