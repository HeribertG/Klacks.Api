// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for Klacksy AgentPlans (Phase 2/3 autonomy roadmap). Lets the frontend create a plan
/// from a free-text goal, list/inspect plans for the current user, approve a paused HITL step, and
/// abort a plan. Plan execution itself is fire-and-forget: the controller kicks off the executor in
/// a fresh service scope and returns immediately while progress is streamed via SignalR PlanUpdated
/// events. Each running execution is tracked in IPlanExecutionRegistry so an abort can cancel it
/// cooperatively between steps.
/// </summary>
/// <param name="planningAgent">Decomposes the goal into PlanStep records.</param>
/// <param name="planRepository">Persists the plan + lookups for list/single endpoints.</param>
/// <param name="executor">Runs the steps one by one with HITL gating.</param>
/// <param name="llmRepository">Resolves the configured default LLM model for provider attribution.</param>
/// <param name="executionRegistry">Tracks running executions so an abort can cancel them.</param>
/// <param name="scopeFactory">Creates a fresh DI scope for the fire-and-forget execution task.</param>
/// <param name="logger">Structured log of controller-side plan lifecycle events.</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/plans")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AgentPlansController : ControllerBase
{
    private readonly IPlanningAgent _planningAgent;
    private readonly IAgentPlanRepository _planRepository;
    private readonly IPlanStepExecutor _executor;
    private readonly ILLMRepository _llmRepository;
    private readonly IPlanExecutionRegistry _executionRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentPlansController> _logger;

    public AgentPlansController(
        IPlanningAgent planningAgent,
        IAgentPlanRepository planRepository,
        IPlanStepExecutor executor,
        ILLMRepository llmRepository,
        IPlanExecutionRegistry executionRegistry,
        IServiceScopeFactory scopeFactory,
        ILogger<AgentPlansController> logger)
    {
        _planningAgent = planningAgent;
        _planRepository = planRepository;
        _executor = executor;
        _llmRepository = llmRepository;
        _executionRegistry = executionRegistry;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAndStartPlan([FromBody] CreatePlanRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal))
        {
            return BadRequest("Goal cannot be empty");
        }

        var defaultModel = await _llmRepository.GetDefaultModelAsync();
        if (defaultModel == null)
        {
            return Conflict("No default LLM model is configured. Set a default model before starting a plan.");
        }

        var userId = GetCurrentUserId();
        var sessionGuid = Guid.TryParse(request.SessionId, out var s) ? s : (Guid?)null;

        var plan = await _planningAgent.CreatePlanAsync(request.Goal, userId, sessionGuid, cancellationToken);
        await _planRepository.AddAsync(plan, cancellationToken);

        var skillContext = BuildSkillExecutionContext(userId, LLMCapabilityService.MapProvider(defaultModel.ProviderId));

        StartBackgroundExecution(plan.Id, skillContext, resume: false);

        return Accepted(plan);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveAndContinue(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan == null) return NotFound();

        var userId = GetCurrentUserId();
        if (!string.Equals(plan.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        var defaultModel = await _llmRepository.GetDefaultModelAsync();
        var providerId = defaultModel != null ? LLMCapabilityService.MapProvider(defaultModel.ProviderId) : null;
        var skillContext = BuildSkillExecutionContext(userId, providerId);

        StartBackgroundExecution(id, skillContext, resume: true);

        return Accepted(plan);
    }

    [HttpPost("{id:guid}/abort")]
    public async Task<IActionResult> AbortPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan == null) return NotFound();

        var userId = GetCurrentUserId();
        if (!string.Equals(plan.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        if (PlanStatus.IsTerminal(plan.Status))
        {
            return Conflict($"Plan is already {plan.Status} and cannot be aborted.");
        }

        if (_executionRegistry.TryRequestCancellation(id))
        {
            return Accepted(plan);
        }

        var aborted = await _executor.AbortAsync(id, cancellationToken);
        if (aborted == null)
        {
            return Conflict("Plan is already in a terminal state and cannot be aborted.");
        }

        return Ok(aborted);
    }

    [HttpGet]
    public async Task<IActionResult> ListMyPlans(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var plans = await _planRepository.ListByUserAsync(userId, cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(id, cancellationToken);
        if (plan == null) return NotFound();

        var userId = GetCurrentUserId();
        if (!string.Equals(plan.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        return Ok(plan);
    }

    private void StartBackgroundExecution(Guid planId, SkillExecutionContext skillContext, bool resume)
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

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Builds the skill-execution context from the caller's claims. TenantId is intentionally
    /// Guid.Empty: Klacks has no row-level tenancy — data is scoped per deployment and, within a
    /// deployment, via GroupVisibility, not a tenant key. Every skill entry point (chat, scheduled
    /// tasks, email actions) uses the same sentinel, so plans stay consistent with them.
    /// </summary>
    /// <param name="userId">Authenticated user id from the NameIdentifier claim.</param>
    /// <param name="providerId">Provider of the configured default LLM model, for usage attribution; null when unmapped.</param>
    private SkillExecutionContext BuildSkillExecutionContext(string userId, LLMProviderType? providerId)
    {
        var permissions = new List<string>();
        foreach (var roleClaim in User.FindAll(ClaimTypes.Role))
        {
            permissions.Add(roleClaim.Value);
            foreach (var permission in Permissions.GetPermissionsForRole(roleClaim.Value))
            {
                if (!permissions.Contains(permission))
                {
                    permissions.Add(permission);
                }
            }
        }

        return new SkillExecutionContext
        {
            UserId = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty,
            TenantId = Guid.Empty,
            UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? userId,
            UserPermissions = permissions,
            ProviderId = providerId
        };
    }
}
