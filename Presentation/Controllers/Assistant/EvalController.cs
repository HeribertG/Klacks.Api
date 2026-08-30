// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assistant evaluation and telemetry-feedback endpoints: admin-triggered goldset runs plus the
/// user-facing feedback routes (thumbs-up, correction, UiAction outcome report).
/// </summary>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant.Evaluation;
using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Extensions;
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
    private const int DefaultCandidateDays = 30;
    private const int MaxCandidateDays = 365;
    private const int DefaultCandidateLimit = 100;
    private const int MaxCandidateLimit = 500;

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
            _logger.LogError(ex, "EvalRunner failed for goldset {Goldset}", goldset.ForLog());
            return StatusCode(500, new { error = "Eval run failed" });
        }
    }

    [HttpPost("run-turn")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<TurnEvalRunResult>>> RunTurn(
        [FromQuery] string goldset,
        [FromQuery] string modelIds,
        [FromQuery] int? maxItems,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(goldset))
        {
            return BadRequest(new { error = "goldset query parameter is required" });
        }

        var models = (modelIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        if (models.Count == 0)
        {
            return BadRequest(new { error = "modelIds query parameter is required (comma-separated)" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var results = await _mediator.Send(new RunTurnEvalCommand
            {
                Goldset = goldset,
                ModelIds = models,
                MaxItems = maxItems,
                UserId = userId,
                UserRights = GetCurrentUserRights()
            }, cancellationToken);
            return Ok(results);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TurnEval run failed for goldset {Goldset}", goldset.ForLog());
            return StatusCode(500, new { error = "Turn eval run failed" });
        }
    }

    [HttpGet("goldset-candidates")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<TurnGoldsetItem>>> GoldsetCandidates(
        [FromQuery] int? days,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTurnGoldsetCandidatesQuery
        {
            Days = Math.Clamp(days ?? DefaultCandidateDays, 1, MaxCandidateDays),
            Limit = Math.Clamp(limit ?? DefaultCandidateLimit, 1, MaxCandidateLimit)
        }, cancellationToken);
        return Ok(result);
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

    /// <summary>
    /// The thumbs-up counterpart of the correction endpoint, open to every authenticated user for the
    /// same reason: only the person who asked can say whether the answer helped. Carries no verdict in
    /// the body - a request to this route always means helpful, so nothing a caller sends can turn it
    /// into a negative judgement about somebody else's turn.
    /// </summary>
    [HttpPost("feedback")]
    public async Task<ActionResult<SubmitHelpfulFeedbackResult>> SubmitHelpfulFeedback(
        [FromBody] SubmitHelpfulFeedbackRequest body,
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
            var result = await _mediator.Send(
                new SubmitHelpfulFeedbackCommand
                {
                    UserId = userId,
                    UserMessage = body.UserMessage ?? string.Empty
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// The UiAction counterpart of the feedback endpoint (W1.4): the browser reports whether the UI
    /// action it was handed under the tracking id completed or failed. The body carries the tracking id
    /// from the dispatch response; the caller identity comes from the token.
    /// </summary>
    [HttpPost("ui-action-result")]
    public async Task<ActionResult<ReportUiActionResultResult>> ReportUiActionResult(
        [FromBody] ReportUiActionResultRequest body,
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
            var result = await _mediator.Send(
                new ReportUiActionResultCommand
                {
                    UserId = userId,
                    TrackingId = body.TrackingId,
                    Status = body.Status ?? string.Empty,
                    ErrorMessage = body.ErrorMessage
                },
                cancellationToken);

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

    public sealed class SubmitHelpfulFeedbackRequest
    {
        public string? UserMessage { get; set; }
    }

    public sealed class ReportUiActionResultRequest
    {
        public Guid TrackingId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private List<string> GetCurrentUserRights() => User.GetUserRights();
}
