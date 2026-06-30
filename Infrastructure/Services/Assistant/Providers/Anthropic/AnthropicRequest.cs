// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wire request DTO for the Anthropic Messages API. System is typed as object to support
/// both the legacy plain-string form and the cache-enabled content-block array form required
/// by the prompt-caching beta. Stream enables server-sent-events delivery.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();

    [JsonPropertyName("system")]
    public object? System { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("tools")]
    public List<object>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }
}
