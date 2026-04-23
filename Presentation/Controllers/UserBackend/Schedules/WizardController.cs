// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
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
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/backend/[controller]")]
public sealed class WizardController : ControllerBase
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
}

/// <summary>
/// Request to start a wizard run.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients to plan for</param>
/// <param name="ShiftIds">Optional subset of shift definitions</param>
/// <param name="AnalyseToken">Scenario isolation token (null = main scenario)</param>
/// <param name="TrainingOverrides">Optional tuning-parameter overrides for training/benchmark</param>
public sealed record StartWizardRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken,
    WizardTrainingOverrides? TrainingOverrides = null);

/// <summary>
/// Request to run a synchronous benchmark of the wizard (training/measurement use).
/// The request is scenario-isolated via AnalyseToken; no Work entities are persisted to the main scenario.
/// </summary>
/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients to plan for</param>
/// <param name="ShiftIds">Optional subset of shift definitions</param>
/// <param name="AnalyseToken">Scenario isolation token (recommended: random per benchmark)</param>
/// <param name="TrainingOverrides">Tuning-parameter overrides for this benchmark</param>
public sealed record WizardBenchmarkRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    IReadOnlyList<Guid>? ShiftIds,
    Guid? AnalyseToken,
    WizardTrainingOverrides? TrainingOverrides = null);

public sealed record StartWizardResponse(Guid JobId);

public sealed record CancelWizardRequest(Guid JobId);

public sealed record CancelWizardResponse(bool Cancelled);

public sealed record ApplyWizardRequest(Guid JobId);

public sealed record ApplyWizardResponse(IReadOnlyList<Guid> CreatedWorkIds);
