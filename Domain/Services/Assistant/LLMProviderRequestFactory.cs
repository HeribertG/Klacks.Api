// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the provider requests of a chat turn. Both chat loops (streaming and non-streaming) sent six
/// hand-written request literals that repeated the same eight model/cost fields, so a new field had to be
/// added in six places or it silently reached only one path. Two shapes exist: a tool-less request for a
/// recipe confirmation or ask step, and the ordinary iteration request that carries the toolset, the
/// tool-choice policy and - on the streaming path - the stream flag and the usage callback.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using LLMMessage = Klacks.Api.Domain.Services.Assistant.Providers.LLMMessage;
using LLMUsage = Klacks.Api.Domain.Services.Assistant.Providers.LLMUsage;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class LLMProviderRequestFactory
{
    private const double DefaultTemperature = 0.7;

    /// <summary>
    /// A request with no tools at all, used by the recipe confirmation and ask steps: the model is asked
    /// to phrase one authored question or confirmation and must not be able to call a skill while doing it.
    /// </summary>
    /// <param name="model">Target model, source of the id, the output cap and all cost fields.</param>
    /// <param name="message">The turn's current user message.</param>
    /// <param name="systemPrompt">Stable system-prompt segment eligible for provider prompt caching.</param>
    /// <param name="volatileSystemPrompt">Already-combined volatile segment including the step instruction.</param>
    /// <param name="history">Running conversation history, already fitted to the turn's budget.</param>
    internal static LLMProviderRequest ToolLess(
        LLMModel model,
        string message,
        string systemPrompt,
        string? volatileSystemPrompt,
        List<LLMMessage> history) =>
        Base(model, message, systemPrompt, volatileSystemPrompt, history, new List<LLMFunction>());

    /// <summary>
    /// The ordinary iteration request of the multi-turn tool loop.
    /// </summary>
    /// <param name="model">Target model, source of the id, the output cap and all cost fields.</param>
    /// <param name="message">The turn's current message - the user message, or the formatted tool results.</param>
    /// <param name="systemPrompt">Stable system-prompt segment eligible for provider prompt caching.</param>
    /// <param name="volatileSystemPrompt">Already-combined volatile segment including this iteration's note.</param>
    /// <param name="history">Running conversation history, already fitted to the turn's budget.</param>
    /// <param name="availableFunctions">The toolset for this iteration, narrowed only by the caller.</param>
    /// <param name="toolChoice">Resolved tool-choice policy, null to leave the provider default in place.</param>
    /// <param name="stream">True on the streaming path, where the provider yields tokens.</param>
    /// <param name="onStreamUsage">Streaming usage callback, null on the non-streaming path.</param>
    internal static LLMProviderRequest ForIteration(
        LLMModel model,
        string message,
        string systemPrompt,
        string? volatileSystemPrompt,
        List<LLMMessage> history,
        List<LLMFunction> availableFunctions,
        string? toolChoice,
        bool stream = false,
        Action<LLMUsage>? onStreamUsage = null)
    {
        var request = Base(model, message, systemPrompt, volatileSystemPrompt, history, availableFunctions);
        request.ToolChoice = toolChoice;
        request.Stream = stream;
        request.OnStreamUsage = onStreamUsage;
        return request;
    }

    private static LLMProviderRequest Base(
        LLMModel model,
        string message,
        string systemPrompt,
        string? volatileSystemPrompt,
        List<LLMMessage> history,
        List<LLMFunction> availableFunctions) =>
        new()
        {
            Message = message,
            SystemPrompt = systemPrompt,
            VolatileSystemPrompt = volatileSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = history,
            AvailableFunctions = availableFunctions,
            Temperature = DefaultTemperature,
            MaxTokens = model.MaxTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            CostPerCacheWriteToken = model.CostPerCacheWriteToken,
            CostPerCacheReadToken = model.CostPerCacheReadToken
        };
}
