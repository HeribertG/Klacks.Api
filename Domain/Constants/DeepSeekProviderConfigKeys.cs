// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration keys for the DeepSeek LLM provider. DisableThinking is bound from
/// environment variable LLM__DeepSeek__DisableThinking.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class DeepSeekProviderConfigKeys
{
    public const string DisableThinking = "LLM:DeepSeek:DisableThinking";
}
