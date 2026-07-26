// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Token counters returned by the Anthropic API. The wire format is snake_case, which
/// PropertyNameCaseInsensitive does not bridge, so every field needs an explicit name.
/// InputTokens counts only the uncached remainder: the full prompt is
/// InputTokens + CacheCreationInputTokens + CacheReadInputTokens.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("cache_creation_input_tokens")]
    public int CacheCreationInputTokens { get; set; }

    [JsonPropertyName("cache_read_input_tokens")]
    public int CacheReadInputTokens { get; set; }
}
