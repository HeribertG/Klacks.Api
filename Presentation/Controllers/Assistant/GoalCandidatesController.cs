// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// REST API for the Phase 2 read-only self-directed-goals inbox (see
/// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md): lists the requesting
/// user's own goal candidates and records an approve/reject decision. Both endpoints are gated to
/// planners (Admin/Authorised) via IPlanningAudienceResolver — the second line of defense, since
/// GoalReflectionService already skips non-planner users once delivery is enabled. Listing defaults
/// to Status = Proposed when no status filter is given, so Phase 1 shadow-mode candidates (which are
/// measurement data, not something any user should ever see) never leak through an omitted filter.
/// After a successful approval, this controller — not the decision handler — fires the Phase 3 plan
/// draft in the background when BackgroundServiceOptions.GoalReflectionPlanDrafting is on: the
/// decision handler deliberately has no planning dependency (Phase 2 guarantee, enforced by
/// SetGoalCandidateDecisionCommandHandlerTests), so the orchestration lives here, the same layer that
/// already launches AgentPlansController's fire-and-forget execution via IPlanChatService. For the same
/// reason, the same background task also triggers Phase 4 (IGoalPlanExecutionService.
/// ExecuteForCandidateAsync) once drafting produced a plan: GoalPlanDraftService deliberately has no
/// execution dependency either (Phase 3 guarantee, enforced by
/// GoalPlanDraftServiceTests.Constructor_TakesNoExecutionDependency_Phase3IsDraftOnly), so wiring the
/// Phase 4 trigger here — rather than inside either service — keeps both draft and decision free of an
/// execution dependency while still resolving the fresh scope only once per approval.
/// </summary>
/// <param name="mediator">Dispatches the goal-candidate query and decision command.</param>
/// <param name="planningAudienceResolver">Resolves whether the caller is a planner (Admin/Authorised).</param>
/// <param name="scopeFactory">Creates a fresh DI scope for the fire-and-forget plan-draft/execution task.</param>
/// <param name="backgroundServiceOptions">Gates the plan draft behind GoalReflectionPlanDrafting; the Phase 4 execution gate (GoalReflectionExecution) lives inside IGoalPlanExecutionService itself.</param>
/// <param name="logger">Structured log of background plan-draft failures.</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/goal-candidates")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class GoalCandidatesController : ControllerBase
{
    private const string InvalidDecisionMessage = "Invalid decision. Allowed values: approved, rejected.";

    private readonly IMediator _mediator;
    private readonly IPlanningAudienceResolver _planningAudienceResolver;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundServiceOptions _backgroundServiceOptions;
    private readonly ILogger<GoalCandidatesController> _logger;

    public GoalCandidatesController(
        IMediator mediator,
        IPlanningAudienceResolver planningAudienceResolver,
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundServiceOptions> backgroundServiceOptions,
        ILogger<GoalCandidatesController> logger)
    {
        _mediator = mediator;
        _planningAudienceResolver = planningAudienceResolver;
        _scopeFactory = scopeFactory;
        _backgroundServiceOptions = backgroundServiceOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GoalCandidateDto>>> GetCandidates(
        [FromQuery] string? status,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!await IsPlannerAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        var candidates = await _mediator.Send(new GetGoalCandidatesQuery
        {
            UserId = userId,
            Status = status ?? GoalCandidateStatus.Proposed,
            Take = take
        }, cancellationToken);

        return Ok(candidates);
    }

    [HttpPut("{id:guid}/decision")]
    public async Task<IActionResult> SetDecision(
        Guid id,
        [FromBody] SetGoalCandidateDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!await IsPlannerAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        if (request.Decision != GoalCandidateStatus.Approved && request.Decision != GoalCandidateStatus.Rejected)
        {
            return BadRequest(InvalidDecisionMessage);
        }

        var found = await _mediator.Send(new SetGoalCandidateDecisionCommand
        {
            Id = id,
            UserId = userId,
            Decision = request.Decision,
            UserPermissions = GetCurrentUserPermissions()
        }, cancellationToken);

        if (found && request.Decision == GoalCandidateStatus.Approved && _backgroundServiceOptions.GoalReflectionPlanDrafting)
        {
            TriggerPlanDraftInBackground(id);
        }

        return found ? NoContent() : NotFound();
    }

    private void TriggerPlanDraftInBackground(Guid candidateId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var draftService = scope.ServiceProvider.GetRequiredService<IGoalPlanDraftService>();
                var planId = await draftService.DraftForCandidateAsync(candidateId, CancellationToken.None);
                if (planId != null)
                {
                    var executionService = scope.ServiceProvider.GetRequiredService<IGoalPlanExecutionService>();
                    await executionService.ExecuteForCandidateAsync(candidateId, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background plan draft/execution failed for goal candidate {CandidateId}", candidateId);
            }
        }, CancellationToken.None);
    }

    private async Task<bool> IsPlannerAsync(string userId, CancellationToken cancellationToken)
    {
        var plannerIds = await _planningAudienceResolver.GetPlanningUserIdsAsync(cancellationToken);
        return plannerIds.Contains(userId);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private List<string> GetCurrentUserPermissions() => User.GetUserRights();
}
