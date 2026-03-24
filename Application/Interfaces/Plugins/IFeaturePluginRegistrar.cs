// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Interface for feature plugins to register their DI services conditionally.
/// Implementations are discovered via assembly scan and invoked only when the plugin is installed.
/// </summary>
/// <param name="PluginName">Must match the plugin manifest name</param>

namespace Klacks.Api.Application.Interfaces.Plugins;

public interface IFeaturePluginRegistrar
{
    string PluginName { get; }
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
