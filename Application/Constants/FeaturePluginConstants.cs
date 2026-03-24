// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for feature plugin discovery, installation and configuration.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class FeaturePluginConstants
{
    public const string PluginDirectory = "Plugins/Features";
    public const string ManifestFileName = "manifest.json";
    public const string SkillSeedsFileName = "skill-seeds.json";
    public const string SettingPrefix = "FEATURE_PLUGIN_";
    public const string EnabledSuffix = "_ENABLED";
    public const string I18nDirectory = "i18n";
}
