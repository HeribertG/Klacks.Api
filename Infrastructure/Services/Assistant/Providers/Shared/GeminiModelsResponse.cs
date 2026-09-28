// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Response DTO for the Gemini GET /v1beta/models endpoint.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

internal sealed class GeminiModelsResponse
{
    [JsonPropertyName("models")]
    public List<GeminiModelEntry> Models { get; set; } = [];
}

internal sealed class GeminiModelEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}
