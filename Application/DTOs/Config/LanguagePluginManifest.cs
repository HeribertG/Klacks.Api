// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Config;

public class LanguagePluginManifest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? SpeechLocale { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public int Coverage { get; set; }
}
