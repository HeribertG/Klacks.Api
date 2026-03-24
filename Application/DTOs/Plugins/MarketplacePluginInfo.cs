// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO representing a feature plugin from the marketplace for browsing and installation.
/// </summary>
/// <param name="Name">Unique plugin identifier</param>
/// <param name="Category">Plugin category (communication, erp, etc.)</param>
/// <param name="RequiredPermissions">Permissions needed to use the plugin</param>
/// <param name="ProvidedSkills">Skills provided by the plugin</param>
namespace Klacks.Api.Application.DTOs.Plugins;

public class MarketplacePluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MinKlacksVersion { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public string[] RequiredPermissions { get; set; } = [];
    public string[] ProvidedSkills { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}
