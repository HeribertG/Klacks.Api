// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    public WizardController(
        IWizardJobRunner runner,
        IWizardApplyService applyService,
        IWizardBenchmarkService benchmarkService,
        JobTerminalStateCache<WizardJobResultDto> stateCache)
    {
        _runner = runner;
        _applyService = applyService;
        _benchmarkService = benchmarkService;
        _stateCache = stateCache;
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartWizardResponse>> Start(
        [FromBody] StartWizardRequest request)
    {
        if (TryBuildLimitError(request, out var error))
        {
            return BadRequest(error);
        }

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

    private static bool TryBuildLimitError(StartWizardRequest request, out WizardLimitErrorResponse error)
    {
        var agents = request.AgentIds?.Count ?? 0;
        var shifts = request.ShiftIds?.Count ?? 0;

        if (agents <= WizardLimits.MaxAgents && shifts <= WizardLimits.MaxShifts)
        {
            error = default!;
            return false;
        }

        error = new WizardLimitErrorResponse(
            Code: WizardLimits.TooLargeErrorCode,
            Message: "Wizard input exceeds supported limits.",
            Agents: agents,
            Shifts: shifts,
            MaxAgents: WizardLimits.MaxAgents,
            MaxShifts: WizardLimits.MaxShifts);
        return true;
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
