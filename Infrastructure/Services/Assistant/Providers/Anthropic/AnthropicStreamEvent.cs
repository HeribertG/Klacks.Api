// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    [JsonPropertyName("message")]
    public AnthropicStreamMessage? Message { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}
