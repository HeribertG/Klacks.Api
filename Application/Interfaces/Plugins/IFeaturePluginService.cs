// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service interface for feature plugin lifecycle management: discovery, installation, activation.
/// </summary>

using Klacks.Api.Application.DTOs.Plugins;

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IFeaturePluginService
{
    Task InitializeAsync();
    Task SyncNavigationRoutesAsync();
    Task<IReadOnlyList<FeaturePluginInfo>> GetAllPluginsAsync();
    Task<FeaturePluginInfo?> GetPluginAsync(string name);
    Task<bool> InstallAsync(string name);
    Task<bool> UninstallAsync(string name);
    Task<bool> EnableAsync(string name);
    Task<bool> DisableAsync(string name);
    bool IsEnabled(string name);

    /// <summary>Whether the plugin's install flag is set. Together with IsEnabled this is exactly what the Angular FeaturePluginStateService.isPluginEnabled asks — deliberately without the operational check, so nothing is stricter than a click.</summary>
    bool IsInstalled(string name);

    /// <summary>Whether a manifest with this name was found on disk during discovery. False means the name is unknown to this installation, not that the plugin is switched off.</summary>
    bool IsDiscovered(string name);
    Task RefreshPluginsAsync();
    Dictionary<string, string>? GetTranslations(string lang);
}
