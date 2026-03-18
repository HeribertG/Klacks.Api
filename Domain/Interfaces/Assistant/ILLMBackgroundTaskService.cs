// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Service für asynchrone Hintergrundaufgaben nach LLM-Interaktionen.
/// </summary>
public interface ILLMBackgroundTaskService
{
    void RunBackgroundTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> allFunctionCalls);
}
