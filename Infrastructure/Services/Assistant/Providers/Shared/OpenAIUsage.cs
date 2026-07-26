// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Token counters returned by OpenAI-compatible APIs. The wire format is snake_case, which neither
/// the camelCase naming policy nor PropertyNameCaseInsensitive bridges, so every field needs an
/// explicit name.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// DeepSeek context-caching counter: prompt tokens served from the server-side cache. Unlike
    /// Anthropic, PromptTokens already INCLUDES these, so the uncached remainder is the difference.
    /// Zero on providers that do not report cache hits.
    /// </summary>
    [JsonPropertyName("prompt_cache_hit_tokens")]
    public int PromptCacheHitTokens { get; set; }

    [JsonPropertyName("prompt_cache_miss_tokens")]
    public int PromptCacheMissTokens { get; set; }
}
