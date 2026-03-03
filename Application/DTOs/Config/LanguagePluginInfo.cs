// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Config;

public class LanguagePluginInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SpeechLocale { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public double Coverage { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsCore { get; set; }
    public int TranslationCount { get; set; }
}
