// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Services.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the schedule autofill wizard.
/// Start launches a background GA job and returns immediately with a job id. Progress streams via SignalR.
/// Apply materialises the cached scenario into Work entities. Cancel aborts a running job.
/// </summary>
[ApiController]
public sealed class WizardController : BaseController
{
    private readonly IWizardJobRunner _runner;
    private readonly IWizardApplyService _applyService;
    private readonly IWizardBenchmarkService _benchmarkService;

    public WizardController(
        IWizardJobRunner runner,
        IWizardApplyService applyService,
        IWizardBenchmarkService benchmarkService)
    {
        _runner = runner;
        _applyService = applyService;
        _benchmarkService = benchmarkService;
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
                AnalyseToken: request.AnalyseToken,
                TrainingOverrides: request.TrainingOverrides),
            ct);

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

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyAsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyAsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, createdIds) = await _applyService.ApplyAsScenarioAsync(request.JobId, request.GroupId, ct);
            return Ok(new ApplyAsScenarioResponse(scenario.Id, scenario.Token, scenario.Name, scenario.RunGroupId, createdIds));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
