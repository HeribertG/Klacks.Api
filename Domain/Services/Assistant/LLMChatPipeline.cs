// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade that bundles the four chat-pipeline services used by LLMService.
/// </summary>
/// <param name="conversationManager">Manages conversation lifecycle and history persistence</param>
/// <param name="functionExecutor">Executes LLM function calls (skills, UI actions, passthrough)</param>
/// <param name="responseBuilder">Builds LLMResponse objects from provider responses</param>
/// <param name="promptBuilder">Assembles the system prompt for LLM requests</param>

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMChatPipeline
{
    public LLMConversationManager ConversationManager { get; }
    public LLMFunctionExecutor FunctionExecutor { get; }
    public LLMResponseBuilder ResponseBuilder { get; }
    public LLMSystemPromptBuilder PromptBuilder { get; }

    public LLMChatPipeline(
        LLMConversationManager conversationManager,
        LLMFunctionExecutor functionExecutor,
        LLMResponseBuilder responseBuilder,
        LLMSystemPromptBuilder promptBuilder)
    {
        ConversationManager = conversationManager;
        FunctionExecutor = functionExecutor;
        ResponseBuilder = responseBuilder;
        PromptBuilder = promptBuilder;
    }
}
