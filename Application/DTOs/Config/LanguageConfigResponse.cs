// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Config;

public class LanguageConfigResponse
{
    public string[] SupportedLanguages { get; set; } = [];
    public string[] FallbackOrder { get; set; } = [];
    public Dictionary<string, LanguageMetadata> Metadata { get; set; } = new();
}
