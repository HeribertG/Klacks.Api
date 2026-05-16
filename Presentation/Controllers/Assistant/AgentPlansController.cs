// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for Klacksy AgentPlans (Phase 2/3 autonomy roadmap). Lets the frontend create a plan
/// from a free-text goal, list/inspect plans for the current user, and approve a paused HITL step.
/// Plan execution itself is fire-and-forget: the controller kicks off ExecutePlanAsync in a fresh
/// service scope and returns immediately while progress is streamed via SignalR PlanUpdated events.
/// </summary>
/// <param name="planningAgent">Decomposes the goal into PlanStep records.</param>
/// <param name="planRepository">Persists the plan + lookups for list/single endpoints.</param>
/// <param name="executor">Runs the steps one by one with HITL gating.</param>
/// <param name="scopeFactory">Creates a fresh DI scope for the fire-and-forget execution task.</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentPlansController> _logger;

    public AgentPlansController(
        IPlanningAgent planningAgent,
        IAgentPlanRepository planRepository,
        IPlanStepExecutor executor,
        IServiceScopeFactory scopeFactory,
        ILogger<AgentPlansController> logger)
    {
        _planningAgent = planningAgent;
        _planRepository = planRepository;
        _executor = executor;
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

        var userId = GetCurrentUserId();
        var sessionGuid = Guid.TryParse(request.SessionId, out var s) ? s : (Guid?)null;

        var plan = await _planningAgent.CreatePlanAsync(request.Goal, userId, sessionGuid, cancellationToken);
        await _planRepository.AddAsync(plan, cancellationToken);

        var skillContext = BuildSkillExecutionContext(userId);

        _ = Task.Run(() => ExecuteInBackgroundAsync(plan.Id, skillContext), CancellationToken.None);

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

        var skillContext = BuildSkillExecutionContext(userId);
        _ = Task.Run(() => ApproveInBackgroundAsync(id, skillContext), CancellationToken.None);

        return Accepted(plan);
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

    private async Task ExecuteInBackgroundAsync(Guid planId, SkillExecutionContext skillContext)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IPlanStepExecutor>();
            await executor.ExecutePlanAsync(planId, skillContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background plan execution failed for {PlanId}", planId);
        }
    }

    private async Task ApproveInBackgroundAsync(Guid planId, SkillExecutionContext skillContext)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<IPlanStepExecutor>();
            await executor.ApproveAndContinueAsync(planId, skillContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background plan approve+continue failed for {PlanId}", planId);
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private SkillExecutionContext BuildSkillExecutionContext(string userId)
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
            ProviderId = LLMProviderType.OpenAI
        };
    }
}
