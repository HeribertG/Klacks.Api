// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the schedule autofill wizard.
/// Start launches a background GA job and returns immediately with a job id. Progress streams via SignalR.
/// Apply materialises the cached scenario into Work entities. Cancel aborts a running job.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class WizardController : BaseController
{
    private readonly IWizardJobRunner _runner;
    private readonly IWizardApplyService _applyService;
    private readonly IWizardBenchmarkService _benchmarkService;
    private readonly JobTerminalStateCache<WizardJobResultDto> _stateCache;
    private readonly IMediator _mediator;

    public WizardController(
        IWizardJobRunner runner,
        IWizardApplyService applyService,
        IWizardBenchmarkService benchmarkService,
        JobTerminalStateCache<WizardJobResultDto> stateCache,
        IMediator mediator)
    {
        _runner = runner;
        _applyService = applyService;
        _benchmarkService = benchmarkService;
        _stateCache = stateCache;
        _mediator = mediator;
    }

    /// <summary>
    /// Read-only report over the captured wizard runs: how often each engine and apply mode ends up
    /// accepted, how much of a proposal survives untouched, and whether a warm start pays off.
    /// </summary>
    /// <param name="from">Lower period bound; omit for no lower bound.</param>
    /// <param name="until">Upper period bound; omit for no upper bound.</param>
    /// <param name="groupId">
    /// Restrict to one group. A filter, not a permission boundary: the whole controller is admin-only,
    /// and the admin role carries no group restriction in this application - it gates capabilities
    /// (sealing a period, seeing deleted rows), never visibility. Anyone who reaches this endpoint may
    /// already start a run for any group through Start on the same controller.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("CaptureReport")]
    public async Task<ActionResult<WizardRunCaptureReportDto>> CaptureReport(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? until,
        [FromQuery] Guid? groupId,
        CancellationToken ct)
    {
        var report = await _mediator.Send(new GetWizardRunCaptureReportQuery(from, until, groupId), ct);
        return Ok(report);
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartWizardResponse>> Start(
        [FromBody] StartWizardRequest request)
    {
        // The limits live in AutofillStartGuard, which every runner consults - a controller copy could
        // drift from it and would not cover runs started through the AutoWizard chain.

        var jobId = await _runner.StartAsync(
            new WizardContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                ShiftIds: request.ShiftIds,
                AnalyseToken: request.AnalyseToken,
                TrainingOverrides: request.TrainingOverrides,
                AgentOrderIsUserDefined: request.AgentOrderIsUserDefined),
            CancellationToken.None);

        return Ok(new StartWizardResponse(jobId));
    }

    [HttpPost("Benchmark")]
    public async Task<ActionResult<WizardBenchmarkResponse>> Benchmark(
        [FromBody] WizardBenchmarkRequest request,
        CancellationToken ct)
    {
        var result = await _benchmarkService.RunAsync(
            new WizardContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                ShiftIds: request.ShiftIds,
                AnalyseToken: request.AnalyseToken,
                TrainingOverrides: request.TrainingOverrides),
            ct);

        return Ok(result);
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelWizardResponse> Cancel([FromBody] CancelWizardRequest request)
    {
        var cancelled = _runner.TryCancel(request.JobId);
        return Ok(new CancelWizardResponse(cancelled));
    }

    [HttpGet("Status/{jobId:guid}")]
    public ActionResult<WizardJobStatusResponse> Status(Guid jobId)
    {
        if (_runner.IsRunning(jobId))
        {
            return Ok(new WizardJobStatusResponse(WizardJobStatusValues.Running, null, null));
        }

        if (_stateCache.TryGet(jobId, out var status, out var result, out var reason))
        {
            return Ok(new WizardJobStatusResponse(status, result, reason));
        }

        return Ok(new WizardJobStatusResponse(WizardJobStatusValues.Unknown, null, null));
    }

    [HttpPost("Apply")]
    public async Task<ActionResult<ApplyWizardResponse>> Apply(
        [FromBody] ApplyWizardRequest request,
        CancellationToken ct)
    {
        try
        {
            var outcome = await _applyService.ApplyAsync(request.JobId, request.OverrideBlock, ct);
            return Ok(new ApplyWizardResponse(
                outcome.CreatedWorkIds,
                outcome.ComplianceViolations,
                outcome.SkippedPlacements,
                outcome.OverrideApplied));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyAsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyAsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, outcome) = await _applyService.ApplyAsScenarioAsync(
                request.JobId, request.GroupId, request.OverrideBlock, ct);
            return Ok(new ApplyAsScenarioResponse(
                scenario.Id,
                scenario.Token,
                scenario.Name,
                scenario.RunGroupId,
                outcome.CreatedWorkIds,
                outcome.ComplianceViolations,
                outcome.SkippedPlacements,
                outcome.OverrideApplied));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
