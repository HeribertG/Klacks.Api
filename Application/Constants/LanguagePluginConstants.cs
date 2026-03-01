// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class LanguagePluginConstants
{
    public const string PluginDirectory = "Plugins/Languages";
    public const string ManifestFileName = "manifest.json";
    public const string TranslationsFileName = "translations.json";
    public const string DocsDirectory = "docs";
    public const string SettingPrefix = "INSTALLED_LANGUAGE_";

    public static readonly string[] CoreLanguages = ["de", "en", "fr", "it"];
}
