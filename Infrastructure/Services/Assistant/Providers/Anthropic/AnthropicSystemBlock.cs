// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single content block inside the Anthropic system-prompt array used when the
/// prompt-caching beta is active. Setting CacheControl to ephemeral marks this block
/// as eligible for server-side caching.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

internal sealed class AnthropicSystemBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("cache_control")]
    public AnthropicCacheControl? CacheControl { get; set; }
}

internal sealed class AnthropicCacheControl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ephemeral";
}
