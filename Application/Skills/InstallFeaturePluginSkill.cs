// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs a feature plugin by name. An unknown name is answered with the names that actually
/// exist, and an already installed plugin is reported as such instead of being treated as a failure.
/// </summary>
/// <param name="name">Technical name of the plugin to install (required).</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("install_feature_plugin")]
public class InstallFeaturePluginSkill : BaseSkillImplementation
{
    private readonly IFeaturePluginService _featurePluginService;

    public InstallFeaturePluginSkill(IFeaturePluginService featurePluginService)
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

        if (plugin.IsInstalled)
        {
            return SkillResult.SuccessResult(
                new { plugin.Name, plugin.IsInstalled, plugin.IsEnabled },
                $"Plugin '{plugin.DisplayName}' is already installed.");
        }

        if (!await _featurePluginService.InstallAsync(plugin.Name))
        {
            return SkillResult.Error($"Installing plugin '{plugin.Name}' failed.");
        }

        return SkillResult.SuccessResult(
            new { plugin.Name, plugin.DisplayName, IsInstalled = true },
            $"Plugin '{plugin.DisplayName}' installed.");
    }
}
