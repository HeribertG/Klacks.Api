// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for Holistic Harmonizer (LLM-driven schedule harmonizer). The Start endpoint
/// kicks off a background job, returns the JobId immediately, and the actual progress and final
/// result are streamed over SignalR via <see cref="Klacks.Api.Infrastructure.Hubs.HolisticHarmonizerJobHub"/>.
/// Apply uses the existing <c>IHarmonizerApplyService</c> so the scenario flow is identical to Wizard 2.
/// </summary>
/// <param name="jobRunner">Background job orchestrator that broadcasts progress over SignalR.</param>
/// <param name="applyService">Persists the cached run result as a new AnalyseScenario.</param>
/// <param name="modelCheckService">Pings every configured LLM model so the UI can highlight unhealthy ones.</param>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class HolisticHarmonizerController : BaseController
{
    private readonly IHolisticHarmonizerJobRunner _jobRunner;
    private readonly IHolisticHarmonizerApplyService _applyService;
    private readonly HolisticHarmonizerModelCheckService _modelCheckService;
    private readonly JobTerminalStateCache<HolisticHarmonizerRunResponse> _stateCache;

    public HolisticHarmonizerController(
        IHolisticHarmonizerJobRunner jobRunner,
        IHolisticHarmonizerApplyService applyService,
        HolisticHarmonizerModelCheckService modelCheckService,
        JobTerminalStateCache<HolisticHarmonizerRunResponse> stateCache)
    {
        _jobRunner = jobRunner;
        _applyService = applyService;
        _modelCheckService = modelCheckService;
        _stateCache = stateCache;
    }

    [HttpGet("Status/{jobId:guid}")]
    public async Task<ActionResult<HolisticHarmonizerJobStatusResponse>> Status(Guid jobId, CancellationToken ct)
    {
        if (_jobRunner.IsRunning(jobId))
        {
            return Ok(new HolisticHarmonizerJobStatusResponse(WizardJobStatusValues.Running, null, null));
        }

        var state = await _stateCache.TryGetAsync(jobId, ct);
        if (state.Found)
        {
            return Ok(new HolisticHarmonizerJobStatusResponse(state.Status, state.Result, state.Reason));
        }

        return Ok(new HolisticHarmonizerJobStatusResponse(WizardJobStatusValues.Unknown, null, null));
    }

    [HttpPost("CheckAllModels")]
    public async Task<ActionResult<HolisticHarmonizerModelCheckResponse>> CheckAllModels(CancellationToken ct)
    {
        var results = await _modelCheckService.CheckAllAsync(ct);
        var dtos = results
            .Select(r => new HolisticHarmonizerModelCheckDto(
                r.ModelId,
                r.DisplayName,
                r.ProviderId,
                r.IsHealthy,
                r.LatencyMs,
                r.Error,
                r.EvalCompositeScore))
            .ToArray();
        return Ok(new HolisticHarmonizerModelCheckResponse(dtos));
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartHolisticHarmonizerResponse>> Start([FromBody] HolisticHarmonizerRunRequest request)
    {
        var jobId = await _jobRunner.StartAsync(
            new HolisticHarmonizerRunInput(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: request.AnalyseToken,
                Language: request.Language),
            CancellationToken.None);

        return Ok(new StartHolisticHarmonizerResponse(jobId));
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelHolisticHarmonizerResponse> Cancel([FromBody] CancelHolisticHarmonizerRequest request)
    {
        var cancelled = _jobRunner.TryCancel(request.JobId);
        return Ok(new CancelHolisticHarmonizerResponse(cancelled));
    }

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyHolisticHarmonizerAsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyHolisticHarmonizerAsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, createdIds, complianceReport) = await _applyService.ApplyAsScenarioAsync(request.JobId, request.GroupId, ct);
            return Ok(new ApplyHolisticHarmonizerAsScenarioResponse(
                scenario.Id, scenario.Token, scenario.Name, scenario.RunGroupId, createdIds, complianceReport));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
