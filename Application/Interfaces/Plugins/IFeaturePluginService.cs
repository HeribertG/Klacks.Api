// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service interface for feature plugin lifecycle management: discovery, installation, activation.
/// </summary>

using Klacks.Api.Application.DTOs.Plugins;

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IFeaturePluginService
{
    Task InitializeAsync();
    IReadOnlyList<FeaturePluginInfo> GetAllPlugins();
    FeaturePluginInfo? GetPlugin(string name);
    Task<bool> InstallAsync(string name);
    Task<bool> UninstallAsync(string name);
    Task<bool> EnableAsync(string name);
    Task<bool> DisableAsync(string name);
    bool IsEnabled(string name);
    Task RefreshPluginsAsync();
}
