// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 2 skeleton implementation of IPlanningAgent.
/// Today: produces an empty plan in 'drafting' state so the downstream pipeline can be wired up.
/// TODO (next iteration): inject ILLMProvider + KnowledgeIndex, generate steps with a
/// dedicated planning system prompt that references only KnowledgeIndex-top-K skills.
/// </summary>
/// <param name="agentRepository">Resolves the default agent for plan ownership.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanningAgent : IPlanningAgent
{
    private readonly IAgentRepository _agentRepository;

    public PlanningAgent(IAgentRepository agentRepository)
    {
        _agentRepository = agentRepository;
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        var defaultAgent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        return new AgentPlan
        {
            Id = Guid.NewGuid(),
            AgentId = defaultAgent?.Id ?? Guid.Empty,
            UserId = userId,
            SessionId = sessionId,
            Goal = goal,
            StepsJson = "[]",
            Status = "drafting",
            CurrentStepIndex = 0,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = userId
        };
    }
}
