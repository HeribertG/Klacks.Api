// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO for the Anthropic GET /v1/models endpoint.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

internal sealed class AnthropicModelsResponse
{
    [JsonPropertyName("data")]
    public List<AnthropicModelEntry> Data { get; set; } = [];
}

internal sealed class AnthropicModelEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}
