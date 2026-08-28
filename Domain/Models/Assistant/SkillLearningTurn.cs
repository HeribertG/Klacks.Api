// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Everything the case collector needs from a finished chat turn. Deliberately a value of its own rather
/// than the whole LLMContext, so the collector cannot reach for the parts of a turn it must never
/// persist.
/// </summary>
/// <param name="ToolNames">Names of the tools that were offered to the model this turn</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningTurn(
    Guid AgentId,
    string UserMessage,
    string AssistantResponse,
    bool HadFunctionCalls,
    string? UserId,
    string? ConversationId,
    string? Language,
    string? ChosenSkill,
    IReadOnlyList<string> ToolNames);
