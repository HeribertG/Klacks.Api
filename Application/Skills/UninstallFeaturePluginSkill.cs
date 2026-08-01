// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Uninstalls a feature plugin by name. An unknown name is answered with the names that actually
/// exist, and a plugin that is not installed is reported as such instead of being treated as a
/// failure.
/// </summary>
/// <param name="name">Technical name of the plugin to uninstall (required).</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("uninstall_feature_plugin")]
public class UninstallFeaturePluginSkill : BaseSkillImplementation
{
    private readonly IFeaturePluginService _featurePluginService;

    public UninstallFeaturePluginSkill(IFeaturePluginService featurePluginService)
    {
        _featurePluginService = featurePluginService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Parameter 'name' is required.");
        }

        var plugins = await _featurePluginService.GetAllPluginsAsync();
        var plugin = plugins.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (plugin == null)
        {
            return SkillResult.Error(
                $"Unknown plugin '{name}'. Available plugins: {string.Join(", ", plugins.Select(p => p.Name))}");
        }

        if (!plugin.IsInstalled)
        {
            return SkillResult.SuccessResult(
                new { plugin.Name, IsInstalled = false },
                $"Plugin '{plugin.DisplayName}' is not installed, so there was nothing to remove.");
        }

        if (!await _featurePluginService.UninstallAsync(plugin.Name))
        {
            return SkillResult.Error($"Uninstalling plugin '{plugin.Name}' failed.");
        }

        return SkillResult.SuccessResult(
            new { plugin.Name, plugin.DisplayName, IsInstalled = false },
            $"Plugin '{plugin.DisplayName}' uninstalled.");
    }
}
