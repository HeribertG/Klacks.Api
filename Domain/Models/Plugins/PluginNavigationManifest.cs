// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Navigation metadata for a feature plugin, describing its sidebar icon and route.
/// </summary>
/// <param name="Route">Router path for navigation (e.g. "/workplace/messaging")</param>
/// <param name="LabelKey">i18n translation key for the tooltip</param>
/// <param name="Position">Sort order in the sidebar plugin section</param>
/// <param name="ViewBox">SVG viewBox attribute</param>
/// <param name="SvgPaths">Array of SVG path definitions</param>

namespace Klacks.Api.Domain.Models.Plugins;

public class PluginNavigationManifest
{
    public string Route { get; set; } = string.Empty;
    public string LabelKey { get; set; } = string.Empty;
    public int Position { get; set; }
    public string ViewBox { get; set; } = "0 0 24 24";
    public PluginSvgPath[] SvgPaths { get; set; } = [];
}

public class PluginSvgPath
{
    public string D { get; set; } = string.Empty;
    public string? Opacity { get; set; }
}
