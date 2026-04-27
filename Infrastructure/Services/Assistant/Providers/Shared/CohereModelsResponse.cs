// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO for the Cohere GET /v1/models endpoint.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

internal sealed class CohereModelsResponse
{
    [JsonPropertyName("models")]
    public List<CohereModelEntry> Models { get; set; } = [];
}

internal sealed class CohereModelEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
