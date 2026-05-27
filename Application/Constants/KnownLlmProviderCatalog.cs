// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated catalog of well-known OpenAI-compatible LLM providers with verified base URLs.
/// Used as the trusted source for provider discovery; base URLs carry a trailing slash so
/// the connectivity test ("models" endpoint) resolves correctly.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class KnownLlmProviderCatalog
{
    public static IReadOnlyList<KnownLlmProviderEntry> Entries { get; } = new List<KnownLlmProviderEntry>
    {
        new("openai", "OpenAI", "https://api.openai.com/v1/", true, "https://platform.openai.com/docs/api-reference"),
        new("anthropic", "Anthropic", "https://api.anthropic.com/v1/", true, "https://docs.anthropic.com/en/api"),
        new("google", "Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai/", true, "https://ai.google.dev/gemini-api/docs/openai"),
        new("mistral", "Mistral AI", "https://api.mistral.ai/v1/", true, "https://docs.mistral.ai/api/"),
        new("deepseek", "DeepSeek", "https://api.deepseek.com/v1/", true, "https://api-docs.deepseek.com/"),
        new("groq", "Groq", "https://api.groq.com/openai/v1/", true, "https://console.groq.com/docs/openai"),
        new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1/", true, "https://openrouter.ai/docs"),
        new("together", "Together AI", "https://api.together.xyz/v1/", true, "https://docs.together.ai/docs/openai-api-compatibility"),
        new("fireworks", "Fireworks AI", "https://api.fireworks.ai/inference/v1/", true, "https://docs.fireworks.ai/"),
        new("xai", "xAI (Grok)", "https://api.x.ai/v1/", true, "https://docs.x.ai/"),
        new("cerebras", "Cerebras", "https://api.cerebras.ai/v1/", true, "https://inference-docs.cerebras.ai/"),
        new("perplexity", "Perplexity", "https://api.perplexity.ai/", true, "https://docs.perplexity.ai/"),
        new("cohere", "Cohere", "https://api.cohere.ai/compatibility/v1/", true, "https://docs.cohere.com/docs/compatibility-api")
    };
}
