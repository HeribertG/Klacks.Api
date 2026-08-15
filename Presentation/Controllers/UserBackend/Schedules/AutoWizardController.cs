// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the AutoWizard orchestrator. Start launches a single background job
/// that runs Wizard 1, Harmonizer (Wizard 2) and Holistic Harmonizer (Wizard 3) sequentially
/// and emits one final SignalR event when the chain finishes. Admin role is required because
/// the chain mutates the schedule and may consume LLM credits.
/// </summary>
/// <param name="runner">Background orchestrator that drives the three sequential stages.</param>
[ApiController]
[Route("api/backend/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class AutoWizardController : ControllerBase
{
    private readonly IAutoWizardJobRunner _runner;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _stateCache;

    public AutoWizardController(
        IAutoWizardJobRunner runner,
        JobTerminalStateCache<AutoWizardJobResultDto> stateCache)
    {
        _runner = runner;
        _stateCache = stateCache;
    }

    [HttpGet("Status/{jobId:guid}")]
    public async Task<ActionResult<AutoWizardJobStatusResponse>> Status(Guid jobId, CancellationToken ct)
    {
        if (_runner.IsRunning(jobId))
        {
            return Ok(new AutoWizardJobStatusResponse(WizardJobStatusValues.Running, null, null));
        }

        var state = await _stateCache.TryGetAsync(jobId, ct);
        if (state.Found)
        {
            return Ok(new AutoWizardJobStatusResponse(state.Status, state.Result, state.Reason));
        }

        return Ok(new AutoWizardJobStatusResponse(WizardJobStatusValues.Unknown, null, null));
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartAutoWizardResponse>> Start(
        [FromBody] StartAutoWizardRequest request)
    {
        // The limits live in AutofillStartGuard, which every runner consults - a controller copy could
        // drift from it and would not cover the chained runs the AutoWizard starts itself.
        var jobId = await _runner.StartAsync(request, CancellationToken.None);
        return Ok(new StartAutoWizardResponse(jobId));
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelAutoWizardResponse> Cancel([FromBody] CancelAutoWizardRequest request)
    {
        var cancelled = _runner.TryCancel(request.JobId);
        return Ok(new CancelAutoWizardResponse(cancelled));
    }
}
