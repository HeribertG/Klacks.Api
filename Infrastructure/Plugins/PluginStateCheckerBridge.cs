// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IPluginStateChecker to the Core IFeaturePluginService.
/// </summary>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginStateCheckerBridge : IPluginStateChecker
{
    private readonly IFeaturePluginService _featurePluginService;

    public PluginStateCheckerBridge(IFeaturePluginService featurePluginService)
    {
        _featurePluginService = featurePluginService;
    }

    public bool IsEnabled(string pluginName)
    {
        return _featurePluginService.IsEnabled(pluginName);
    }
}
