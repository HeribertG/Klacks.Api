// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// DTO for a language package from the Marketplace.
/// </summary>
namespace Klacks.Api.Application.DTOs.Config;

public class MarketplacePackageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SpeechLocale { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public double Coverage { get; set; }
    public int TranslationCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public string MinKlacksVersion { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}
