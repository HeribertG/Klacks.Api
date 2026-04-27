// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO for the OpenAI-compatible GET /models endpoint.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

internal sealed class OpenAIModelsResponse
{
    [JsonPropertyName("data")]
    public List<OpenAIModelEntry> Data { get; set; } = [];
}

internal sealed class OpenAIModelEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
