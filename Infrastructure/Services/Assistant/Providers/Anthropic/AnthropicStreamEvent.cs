// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal DTOs for deserializing Anthropic SSE streaming events.
/// Only the fields consumed by AnthropicProvider.ProcessStreamAsync are mapped.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

internal sealed class AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("delta")]
    public AnthropicStreamDelta? Delta { get; set; }

    [JsonPropertyName("content_block")]
    public AnthropicStreamContentBlock? ContentBlock { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

internal sealed class AnthropicStreamDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("partial_json")]
    public string? PartialJson { get; set; }
}

internal sealed class AnthropicStreamContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
