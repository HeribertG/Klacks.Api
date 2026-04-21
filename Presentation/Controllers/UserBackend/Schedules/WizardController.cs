// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the schedule autofill wizard.
/// Start launches a background GA job and returns immediately with a job id. Progress streams via SignalR.
/// Apply materialises the cached scenario into Work entities. Cancel aborts a running job.
/// </summary>
[ApiController]
[Authorize]
[Route("api/backend/[controller]")]
public sealed class WizardController : ControllerBase
{
    private readonly IWizardJobRunner _runner;
    private readonly IWizardApplyService _applyService;

    public WizardController(IWizardJobRunner runner, IWizardApplyService applyService)
    {
        _runner = runner;
        _applyService = applyService;
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartWizardResponse>> Start(
        [FromBody] StartWizardRequest request,
        CancellationToken ct)
    {
        var jobId = await _runner.StartAsync(
            new WizardContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                ShiftIds: request.ShiftIds,
                AnalyseToken: request.AnalyseToken),
            ct);

        return Ok(new StartWizardResponse(jobId));
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelWizardResponse> Cancel([FromBody] CancelWizardRequest request)
    {
        var cancelled = _runner.TryCancel(request.JobId);
        return Ok(new CancelWizardResponse(cancelled));
    }

    [HttpPost("Apply")]
    public async Task<ActionResult<ApplyWizardResponse>> Apply(
        [FromBody] ApplyWizardRequest request,
        CancellationToken ct)
    {
        try
        {
            var createdIds = await _applyService.ApplyAsync(request.JobId, ct);
            return Ok(new ApplyWizardResponse(createdIds));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request to start a wizard run.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients to plan for</param>
/// <param name="ShiftIds">Optional subset of shift definitions</param>
/// <param name="AnalyseToken">Scenario isolation token (null = main scenario)</param>
public sealed record StartWizardRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken);

public sealed record StartWizardResponse(Guid JobId);

public sealed record CancelWizardRequest(Guid JobId);

public sealed record CancelWizardResponse(bool Cancelled);

public sealed record ApplyWizardRequest(Guid JobId);

public sealed record ApplyWizardResponse(IReadOnlyList<Guid> CreatedWorkIds);
