// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manifest model for a feature plugin, describing its metadata and requirements.
/// </summary>

namespace Klacks.Api.Domain.Models.Plugins;

public class FeaturePluginManifest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MinKlacksVersion { get; set; } = "1.0.0";
    public string[] RequiredPermissions { get; set; } = [];
    public string[] ProvidedSkills { get; set; } = [];
    public Dictionary<string, string> DefaultSettings { get; set; } = new();
    public PluginNavigationManifest? Navigation { get; set; }
}
