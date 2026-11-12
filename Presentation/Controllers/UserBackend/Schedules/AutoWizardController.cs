// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// REST entry point for the AutoWizard orchestrator (the autofill of the schedule). Start launches a single background
/// job that runs Wizard 1 and the Holistic Harmonizer (Wizard 3) sequentially and emits one final
/// SignalR event when the chain finishes. Autofill is planning work, so it needs the planning right CanEditSchedule
/// (policy RequireScheduleEditing, the same right as its skills), which every role including the role-less planner floor holds.
/// Group visibility bounds it: the runner refuses a start for a group the caller may not see, and Status and Cancel answer
/// a job the caller may not address (IAutoWizardJobAccessGuard) exactly like an unknown one.
/// </summary>
/// <param name="runner">Background orchestrator that drives the sequential stages.</param>
/// <param name="stateCache">Terminal outcomes of finished jobs.</param>
/// <param name="jobAccessGuard">Decides whether the caller may address a job.</param>
[ApiController]
[Route("api/backend/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthorizationPolicies.RequireScheduleEditing)]
public sealed class AutoWizardController : ControllerBase
{
    private readonly IAutoWizardJobRunner _runner;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _stateCache;
    private readonly IAutoWizardJobAccessGuard _jobAccessGuard;

    public AutoWizardController(
        IAutoWizardJobRunner runner,
        JobTerminalStateCache<AutoWizardJobResultDto> stateCache,
        IAutoWizardJobAccessGuard jobAccessGuard)
    {
        _runner = runner;
        _stateCache = stateCache;
        _jobAccessGuard = jobAccessGuard;
    }

    [HttpGet("Status/{jobId:guid}")]
    public async Task<ActionResult<AutoWizardJobStatusResponse>> Status(Guid jobId, CancellationToken ct)
    {
        if (!await _jobAccessGuard.CanAccessAsync(jobId, ct))
        {
            return Ok(new AutoWizardJobStatusResponse(WizardJobStatusValues.Unknown, null, null));
        }

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
    public async Task<ActionResult<CancelAutoWizardResponse>> Cancel(
        [FromBody] CancelAutoWizardRequest request, CancellationToken ct)
    {
        var cancelled = await _jobAccessGuard.CanAccessAsync(request.JobId, ct) && _runner.TryCancel(request.JobId);
        return Ok(new CancelAutoWizardResponse(cancelled));
    }
}
