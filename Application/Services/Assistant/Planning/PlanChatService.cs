// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IPlanChatService. Extracts the create-and-start plan logic shared by AgentPlansController
/// and the create_plan chat skill: it decomposes the goal via the PlanningAgent and persists the
/// draft plan (no execution), resolves the default model's provider for usage attribution, and
/// launches the fire-and-forget executor in a fresh DI scope tracked in the singleton execution
/// registry so an abort can cancel it between steps.
/// </summary>
/// <param name="planningAgent">Decomposes the goal into PlanStep records.</param>
/// <param name="planRepository">Persists the drafted plan.</param>
/// <param name="llmRepository">Resolves the configured default LLM model for provider attribution.</param>
/// <param name="executionRegistry">Tracks running executions so an abort can cancel them.</param>
/// <param name="scopeFactory">Creates a fresh DI scope for the fire-and-forget execution task.</param>
/// <param name="logger">Structured log of plan lifecycle events.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public class PlanChatService : IPlanChatService
{
    private readonly IPlanningAgent _planningAgent;
    private readonly IAgentPlanRepository _planRepository;
    private readonly ILLMRepository _llmRepository;
    private readonly IPlanExecutionRegistry _executionRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlanChatService> _logger;

    public PlanChatService(
        IPlanningAgent planningAgent,
        IAgentPlanRepository planRepository,
        ILLMRepository llmRepository,
        IPlanExecutionRegistry executionRegistry,
        IServiceScopeFactory scopeFactory,
        ILogger<PlanChatService> logger)
    {
        _planningAgent = planningAgent;
        _planRepository = planRepository;
        _llmRepository = llmRepository;
        _executionRegistry = executionRegistry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<AgentPlan> CreatePlanAsync(
        string goal,
        string userId,
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planningAgent.CreatePlanAsync(goal, userId, sessionId, cancellationToken);
        await _planRepository.AddAsync(plan, cancellationToken);
        return plan;
    }

    public async Task<PlanProviderResolution> ResolveExecutionProviderAsync(CancellationToken cancellationToken = default)
    {
        var defaultModel = await _llmRepository.GetDefaultModelAsync();
        if (defaultModel == null)
        {
            return new PlanProviderResolution(false, null);
        }

        return new PlanProviderResolution(true, LLMCapabilityService.MapProvider(defaultModel.ProviderId));
    }

    public void StartBackgroundExecution(Guid planId, SkillExecutionContext skillContext, bool resume)
    {
        var token = _executionRegistry.Register(planId);
        _ = Task.Run(() => ExecuteInBackgroundAsync(planId, skillContext, resume, token), CancellationToken.None);
    }

    private async Task ExecuteInBackgroundAsync(
        Guid planId, SkillExecutionContext skillContext, bool resume, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IPlanStepExecutor>();
            if (resume)
            {
                await executor.ApproveAndContinueAsync(planId, skillContext, cancellationToken);
            }
            else
            {
                await executor.ExecutePlanAsync(planId, skillContext, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background plan execution cancelled for {PlanId}", planId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background plan execution failed for {PlanId}", planId);
        }
        finally
        {
            _executionRegistry.Unregister(planId);
        }
    }
}
