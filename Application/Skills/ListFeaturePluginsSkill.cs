// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the feature plugins known to the system with their install and enable state, so the agent
/// learns the exact plugin names the other feature-plugin skills expect.
/// </summary>
/// <param name="onlyInstalled">When true, plugins that are not installed are left out.</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_feature_plugins")]
public class ListFeaturePluginsSkill : BaseSkillImplementation
{
    private readonly IFeaturePluginService _featurePluginService;

    public ListFeaturePluginsSkill(IFeaturePluginService featurePluginService)
    {
        _featurePluginService = featurePluginService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var plugins = await _featurePluginService.GetAllPluginsAsync();

        var onlyInstalled = GetParameter<bool?>(parameters, "onlyInstalled") ?? false;

        var projected = plugins
            .Where(p => !onlyInstalled || p.IsInstalled)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new
            {
                p.Name,
                p.DisplayName,
                p.Category,
                p.Version,
                p.Description,
                p.IsInstalled,
                p.IsEnabled,
                p.IsOperational
            })
            .ToList();

        var installed = plugins.Count(p => p.IsInstalled);

        return SkillResult.SuccessResult(
            new { Count = projected.Count, InstalledCount = installed, Plugins = projected },
            $"{projected.Count} feature plugin(s) listed, {installed} of them installed.");
    }
}
