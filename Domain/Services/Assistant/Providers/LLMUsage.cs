// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public class LLMUsage
{
    /// <summary>
    /// Prompt tokens billed at the full uncached rate. With prompt caching active this is only the
    /// remainder — the cached portion is reported separately and must be added for the real prompt size.
    /// </summary>
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens => InputTokens + CacheCreationInputTokens + CacheReadInputTokens + OutputTokens;

    public decimal Cost { get; set; }

    /// <summary>
    /// Prompt tokens written into the provider's prompt cache on this call (a cache miss that
    /// pays a write premium). Zero for providers without prompt caching.
    /// </summary>
    public int CacheCreationInputTokens { get; set; }

    /// <summary>
    /// Prompt tokens served from the provider's prompt cache on this call (a cache hit, billed at
    /// a fraction of the uncached input rate). Zero for providers without prompt caching.
    /// </summary>
    public int CacheReadInputTokens { get; set; }
}