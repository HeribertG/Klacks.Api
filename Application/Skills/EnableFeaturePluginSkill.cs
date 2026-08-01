// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches an installed feature plugin on. A plugin that is not installed cannot be switched on, so
/// that case is rejected with a message naming the missing step rather than a bare failure.
/// </summary>
/// <param name="name">Technical name of the plugin to switch on (required).</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("enable_feature_plugin")]
public class EnableFeaturePluginSkill : BaseSkillImplementation
{
    private readonly IFeaturePluginService _featurePluginService;

    public EnableFeaturePluginSkill(IFeaturePluginService featurePluginService)
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
            return SkillResult.Error(
                $"Plugin '{plugin.DisplayName}' is not installed yet — install it before switching it on.");
        }

        if (plugin.IsEnabled)
        {
            return SkillResult.SuccessResult(
                new { plugin.Name, IsEnabled = true },
                $"Plugin '{plugin.DisplayName}' is already switched on.");
        }

        if (!await _featurePluginService.EnableAsync(plugin.Name))
        {
            return SkillResult.Error($"Switching plugin '{plugin.Name}' on failed.");
        }

        return SkillResult.SuccessResult(
            new { plugin.Name, plugin.DisplayName, IsEnabled = true },
            $"Plugin '{plugin.DisplayName}' switched on.");
    }
}
