// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches an installed feature plugin off without removing it, so its settings survive and it can
/// be switched on again later.
/// </summary>
/// <param name="name">Technical name of the plugin to switch off (required).</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("disable_feature_plugin")]
public class DisableFeaturePluginSkill : BaseSkillImplementation
{
    private readonly IFeaturePluginService _featurePluginService;

    public DisableFeaturePluginSkill(IFeaturePluginService featurePluginService)
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

        if (!plugin.IsEnabled)
        {
            return SkillResult.SuccessResult(
                new { plugin.Name, IsEnabled = false },
                $"Plugin '{plugin.DisplayName}' is already switched off.");
        }

        if (!await _featurePluginService.DisableAsync(plugin.Name))
        {
            return SkillResult.Error($"Switching plugin '{plugin.Name}' off failed.");
        }

        return SkillResult.SuccessResult(
            new { plugin.Name, plugin.DisplayName, IsEnabled = false },
            $"Plugin '{plugin.DisplayName}' switched off; it stays installed and keeps its settings.");
    }
}
