// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Captures per-turn skill selection telemetry (knowledge-index candidates, chosen skill, latencies).
/// Designed for fire-and-forget invocation from LLMBackgroundTaskService.
/// </summary>
/// <param name="agentId">ID of the agent that handled the turn</param>
/// <param name="context">Context with the user message, locale and available functions presented to the LLM</param>
/// <param name="responseContent">Raw assistant response from the turn</param>
/// <param name="allFunctionCalls">Function calls actually executed by the LLM during the turn</param>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITrajectoryCaptureService
{
    Task CaptureAsync(Guid agentId, LLMContext context, string responseContent, List<LLMFunctionCall> allFunctionCalls);
}
