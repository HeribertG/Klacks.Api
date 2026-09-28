// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Configuration keys for the DeepSeek LLM provider. DisableThinking is bound from
/// environment variable LLM__DeepSeek__DisableThinking.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class DeepSeekProviderConfigKeys
{
    public const string DisableThinking = "LLM:DeepSeek:DisableThinking";
}
