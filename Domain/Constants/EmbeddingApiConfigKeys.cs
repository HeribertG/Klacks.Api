// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Configuration keys for the OpenAI-compatible embeddings HTTP API used by EmbeddingService
/// (AI memory feature).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EmbeddingApiConfigKeys
{
    public const string ApiKey = "LLM:Embedding:ApiKey";
    public const string OpenAiApiKeyFallback = "LLM:OpenAI:ApiKey";
    public const string BaseUrl = "LLM:Embedding:BaseUrl";
    public const string Model = "LLM:Embedding:Model";
}
