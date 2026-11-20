// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the copy of a provider request that goes on the line, with every text field run through a transform. The
/// incoming request is never changed: the stream reader sends the same request again on a transient retry, and the
/// caller keeps using its history. History messages are copied into fresh instances. A test pins that
/// every settable property of <see cref="LLMProviderRequest"/> and <see cref="LLMMessage"/> is carried over.
/// </summary>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static class PseudonymRequestCopier
{
    /// <param name="source">The request as the caller built it</param>
    /// <param name="transform">Applied to the message, the volatile system prompt (memories, page context: user data)
    /// and each history message's content</param>
    /// <param name="systemTransform">Applied to the static, Klacks-authored system prompt; the transform is used when null</param>
    public static LLMProviderRequest Copy(
        LLMProviderRequest source, Func<string, string> transform, Func<string, string>? systemTransform = null)
    {
        systemTransform ??= transform;
        if (source.IsKlacksAuthoredText)
        {
            transform = systemTransform;
        }

        return new LLMProviderRequest
        {
            Message = transform(source.Message),
            SystemPrompt = systemTransform(source.SystemPrompt),
            VolatileSystemPrompt = source.VolatileSystemPrompt == null ? null : transform(source.VolatileSystemPrompt),
            ModelId = source.ModelId,
            SupportedParameters = source.SupportedParameters,
            ConversationHistory = source.ConversationHistory.Select(message => CopyMessage(message, transform)).ToList(),
            AvailableFunctions = source.AvailableFunctions,
            Temperature = source.Temperature,
            MaxTokens = source.MaxTokens,
            CostPerInputToken = source.CostPerInputToken,
            CostPerOutputToken = source.CostPerOutputToken,
            CostPerCacheWriteToken = source.CostPerCacheWriteToken,
            CostPerCacheReadToken = source.CostPerCacheReadToken,
            Stream = source.Stream,
            ToolChoice = source.ToolChoice,
            ImagePng = source.ImagePng,
            ThinkingBudgetTokens = source.ThinkingBudgetTokens,
            OnStreamUsage = source.OnStreamUsage,
            StopSequences = source.StopSequences,
            IsKlacksAuthoredText = source.IsKlacksAuthoredText
        };
    }

    private static LLMMessage CopyMessage(LLMMessage message, Func<string, string> transform)
    {
        return new LLMMessage
        {
            Role = message.Role,
            Content = transform(message.Content),
            Timestamp = message.Timestamp
        };
    }
}
