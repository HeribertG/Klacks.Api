// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.Domain.Services.Assistant.Providers;
using ProviderLLMMessage = Klacks.Api.Domain.Services.Assistant.Providers.LLMMessage;
using ProviderLLMUsage = Klacks.Api.Domain.Services.Assistant.Providers.LLMUsage;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Kontext-Objekt für die Multi-Turn-Loop-Ausführung im LLM-Service.
/// </summary>
/// <param name="Context">Der LLM-Kontext mit User-Info und Conversation-ID</param>
/// <param name="Model">Das verwendete LLM-Modell</param>
/// <param name="Provider">Der LLM-Provider für API-Aufrufe</param>
/// <param name="SystemPrompt">Der System-Prompt für die Konversation</param>
/// <param name="TruncatedHistory">Die gekürzte Chat-Historie</param>
/// <param name="TotalUsage">Akkumulierte Token-Usage</param>
/// <param name="Conversation">Die aktive Konversation</param>
/// <param name="Stopwatch">Zeitmessung für die Ausführung</param>
public record MultiTurnContext(
    LLMContext Context, LLMModel Model, ILLMProvider Provider,
    string SystemPrompt, List<ProviderLLMMessage> TruncatedHistory,
    ProviderLLMUsage TotalUsage, LLMConversation Conversation, Stopwatch Stopwatch);
