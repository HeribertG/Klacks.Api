// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin-only endpoints for the skill-description proposal workflow (Agent C).
/// POST /generate triggers the optimizer, GET /pending lists open proposals,
/// POST /{id}/approve applies the change, POST /{id}/reject discards it.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/skill-proposals")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class SkillProposalsController : ControllerBase
{
    private const int DefaultPendingLimit = 50;
    private const int MaxPendingLimit = 200;
    private const int DefaultTrajectoriesToAnalyze = 30;
    private const int MaxTrajectoriesToAnalyze = 200;

    private readonly ISkillDescriptionOptimizer _optimizer;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<SkillProposalsController> _logger;

    public SkillProposalsController(
        ISkillDescriptionOptimizer optimizer,
        IProposedSkillChangeRepository proposalRepository,
        IMediator mediator,
        ILogger<SkillProposalsController> logger)
    {
        _optimizer = optimizer;
        _proposalRepository = proposalRepository;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateProposalsResponse>> Generate(
        [FromQuery] int? trajectories,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(trajectories ?? DefaultTrajectoriesToAnalyze, 1, MaxTrajectoriesToAnalyze);

        try
        {
            var generated = await _optimizer.GenerateProposalsAsync(limit, cancellationToken);
            return Ok(new GenerateProposalsResponse(generated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill proposal generation failed");
            return StatusCode(500, new { error = "Generation failed" });
        }
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<ProposedSkillChange>>> GetPending(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectiveLimit = Math.Clamp(limit ?? DefaultPendingLimit, 1, MaxPendingLimit);
            var pending = await _proposalRepository.GetPendingAsync(effectiveLimit, cancellationToken);
            return Ok(pending);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApproveProposedSkillChangeResult>> Approve(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new ApproveProposedSkillChangeCommand
        {
            ProposalId = id,
            ReviewedBy = reviewedBy
        });

        if (!result.Applied)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<RejectProposedSkillChangeResult>> Reject(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RejectProposedSkillChangeCommand
        {
            ProposalId = id,
            ReviewedBy = reviewedBy
        });

        if (!result.Rejected)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result);
    }

    public sealed record GenerateProposalsResponse(int Generated);
}
