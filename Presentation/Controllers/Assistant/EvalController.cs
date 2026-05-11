// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin-only endpoint to trigger goldset evaluations and read evaluation history.
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Services.Assistant.Evaluation;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/eval")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EvalController : ControllerBase
{
    private const int DefaultHistoryLimit = 20;
    private const int MaxHistoryLimit = 200;

    private readonly IEvalRunnerService _runner;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<EvalController> _logger;

    public EvalController(
        IEvalRunnerService runner,
        IEvalRunRepository evalRunRepository,
        IMediator mediator,
        ILogger<EvalController> logger)
    {
        _runner = runner;
        _evalRunRepository = evalRunRepository;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("run")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EvalRun>> Run([FromQuery] string goldset, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(goldset))
        {
            return BadRequest(new { error = "goldset query parameter is required" });
        }

        try
        {
            var result = await _runner.RunAsync(goldset, cancellationToken);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EvalRunner failed for goldset {Goldset}", goldset);
            return StatusCode(500, new { error = "Eval run failed" });
        }
    }

    [HttpGet("runs")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<EvalRun>>> History(
        [FromQuery] string goldset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(goldset))
        {
            return BadRequest(new { error = "goldset query parameter is required" });
        }

        var effectiveLimit = Math.Clamp(limit ?? DefaultHistoryLimit, 1, MaxHistoryLimit);
        var history = await _evalRunRepository.GetHistoryAsync(goldset, effectiveLimit, cancellationToken);
        return Ok(history);
    }

    [HttpPost("correction")]
    public async Task<ActionResult<SubmitCorrectionResult>> SubmitCorrection(
        [FromBody] SubmitCorrectionRequest body,
        CancellationToken cancellationToken)
    {
        if (body == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _mediator.Send(new SubmitCorrectionCommand
            {
                UserId = userId,
                UserMessage = body.UserMessage ?? string.Empty,
                CorrectionType = body.CorrectionType ?? string.Empty,
                ExpectedSkill = body.ExpectedSkill
            });
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed class SubmitCorrectionRequest
    {
        public string? UserMessage { get; set; }
        public string? CorrectionType { get; set; }
        public string? ExpectedSkill { get; set; }
    }
}
