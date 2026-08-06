// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for Klacksy AgentPlans (Phase 2/3 autonomy roadmap). Lets the frontend create a plan
/// from a free-text goal, list/inspect plans for the current user, approve a paused HITL step, and
/// abort a plan. Plan creation and fire-and-forget execution are delegated to IPlanChatService, the
/// same service the create_plan chat skill uses, so both entry points share one create-and-start
/// path. Progress is streamed via SignalR PlanUpdated events; each running execution is tracked in
/// IPlanExecutionRegistry so an abort can cancel it cooperatively between steps.
/// </summary>
/// <param name="planChatService">Shared create-and-start plan lifecycle (create, provider resolution, launch).</param>
/// <param name="planRepository">Persists the plan + lookups for list/single endpoints.</param>
/// <param name="executor">Runs the steps one by one with HITL gating.</param>
/// <param name="executionRegistry">Tracks running executions so an abort can cancel them.</param>
/// <param name="logger">Structured log of controller-side plan lifecycle events.</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/plans")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthorizationPolicies.RequireAssistant)]
public class AgentPlansController : ControllerBase
{
    private readonly IPlanChatService _planChatService;
    private readonly IAgentPlanRepository _planRepository;
    private readonly IPlanStepExecutor _executor;
    private readonly IPlanExecutionRegistry _executionRegistry;
    private readonly ILogger<AgentPlansController> _logger;

    public AgentPlansController(
        IPlanChatService planChatService,
        IAgentPlanRepository planRepository,
        IPlanStepExecutor executor,
        IPlanExecutionRegistry executionRegistry,
        ILogger<AgentPlansController> logger)
    {
        _planChatService = planChatService;
        _planRepository = planRepository;
        _executor = executor;
        _executionRegistry = executionRegistry;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAndStartPlan([FromBody] CreatePlanRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goal))
        {
            return BadRequest("Goal cannot be empty");
        }

        var providerResolution = await _planChatService.ResolveExecutionProviderAsync(cancellationToken);
        if (!providerResolution.HasDefaultModel)
        {
            return Conflict("No default LLM model is configured. Set a default model before starting a plan.");
        }

        var userId = GetCurrentUserId();
        var sessionGuid = Guid.TryParse(request.SessionId, out var s) ? s : (Guid?)null;

        var plan = await _planChatService.CreatePlanAsync(request.Goal, userId, sessionGuid, cancellationToken);

        var skillContext = BuildSkillExecutionContext(userId, providerResolution.ProviderId);
        _planChatService.StartBackgroundExecution(plan.Id, skillContext, resume: false);

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

        var providerResolution = await _planChatService.ResolveExecutionProviderAsync(cancellationToken);
        var skillContext = BuildSkillExecutionContext(userId, providerResolution.ProviderId);

        _planChatService.StartBackgroundExecution(id, skillContext, resume: true);

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
        var permissions = User.GetUserRights();

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
