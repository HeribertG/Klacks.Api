// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini;

public class GeminiThinkingConfig
{
    [JsonPropertyName("thinkingBudget")]
    public int ThinkingBudget { get; set; }
}
