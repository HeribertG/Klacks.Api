// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public class LLMProviderResponse
{
    public string Content { get; set; } = string.Empty;
    public List<LLMFunctionCall> FunctionCalls { get; set; } = new();
    public LLMUsage Usage { get; set; } = new();
    public bool Success { get; set; } = true;
    public string? Error { get; set; }

    /// <summary>
    /// True when the model wrote reasoning but no content and no tool call. Content is empty in that
    /// case: reasoning is never surfaced as the answer. Diagnostic only.
    /// </summary>
    public bool ReasoningWithoutContent { get; set; }

    /// <summary>
    /// True when the provider stopped because the output-token limit was reached, so the content may be
    /// empty or cut off. Set by providers that report a finish reason; false otherwise.
    /// </summary>
    public bool OutputTruncated { get; set; }

    /// <summary>
    /// Tokens the model spent on internal reasoning, when the provider reports them; 0 otherwise.
    /// </summary>
    public int ReasoningTokens { get; set; }
}