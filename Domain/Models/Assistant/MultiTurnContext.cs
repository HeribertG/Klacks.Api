// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.Domain.Services.Assistant.Providers;
using ProviderLLMMessage = Klacks.Api.Domain.Services.Assistant.Providers.LLMMessage;
using ProviderLLMUsage = Klacks.Api.Domain.Services.Assistant.Providers.LLMUsage;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Context object for multi-turn loop execution in the LLM service.
/// </summary>
/// <param name="Context">The LLM context with user info and conversation ID</param>
/// <param name="Model">The LLM model being used</param>
/// <param name="Provider">The LLM provider for API calls</param>
/// <param name="SystemPrompt">The system prompt for the conversation</param>
/// <param name="TruncatedHistory">The truncated chat history</param>
/// <param name="TotalUsage">Accumulated token usage</param>
/// <param name="Conversation">The active conversation</param>
/// <param name="Stopwatch">Time measurement for execution</param>
public record MultiTurnContext(
    LLMContext Context, LLMModel Model, ILLMProvider Provider,
    string SystemPrompt, List<ProviderLLMMessage> TruncatedHistory,
    ProviderLLMUsage TotalUsage, LLMConversation Conversation, Stopwatch Stopwatch);
