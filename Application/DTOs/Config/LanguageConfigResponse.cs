// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Config;

public class LanguageConfigResponse
{
    public string[] SupportedLanguages { get; set; } = [];
    public string[] FallbackOrder { get; set; } = [];
    public string DefaultLanguage { get; set; } = LanguageConfig.DefaultLanguageFallback;
    public Dictionary<string, LanguageMetadata> Metadata { get; set; } = new();
}
