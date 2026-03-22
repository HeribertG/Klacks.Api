// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO representing a feature plugin with its manifest data and runtime status.
/// </summary>

namespace Klacks.Api.Application.DTOs.Plugins;

public class FeaturePluginInfo
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
    public bool IsInstalled { get; set; }
    public bool IsEnabled { get; set; }
}
