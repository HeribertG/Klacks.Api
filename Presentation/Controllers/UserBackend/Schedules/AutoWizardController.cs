// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
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

    public AutoWizardController(IAutoWizardJobRunner runner)
    {
        _runner = runner;
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartAutoWizardResponse>> Start(
        [FromBody] StartAutoWizardRequest request,
        CancellationToken ct)
    {
        if (TryBuildLimitError(request, out var error))
        {
            return BadRequest(error);
        }

        var jobId = await _runner.StartAsync(request, ct);
        return Ok(new StartAutoWizardResponse(jobId));
    }

    private static bool TryBuildLimitError(StartAutoWizardRequest request, out AutoWizardLimitErrorResponse error)
    {
        var agents = request.AgentIds?.Count ?? 0;
        var shifts = request.ShiftIds?.Count ?? 0;
        var periodDays = Math.Max(1, request.PeriodUntil.DayNumber - request.PeriodFrom.DayNumber + 1);
        var slotProduct = (long)agents * Math.Max(1, shifts) * periodDays;

        var tooManyAgents = agents > AutoWizardLimits.MaxAgents;
        var tooManyShifts = shifts > AutoWizardLimits.MaxShifts;
        var tooLargeProduct = slotProduct > AutoWizardLimits.MaxSlotProduct;

        if (!tooManyAgents && !tooManyShifts && !tooLargeProduct)
        {
            error = default!;
            return false;
        }

        error = new AutoWizardLimitErrorResponse(
            Code: AutoWizardLimits.TooLargeErrorCode,
            Message: "AutoWizard input exceeds supported limits.",
            Agents: agents,
            Shifts: shifts,
            PeriodDays: periodDays,
            MaxAgents: AutoWizardLimits.MaxAgents,
            MaxShifts: AutoWizardLimits.MaxShifts,
            MaxSlotProduct: AutoWizardLimits.MaxSlotProduct);
        return true;
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelAutoWizardResponse> Cancel([FromBody] CancelAutoWizardRequest request)
    {
        var cancelled = _runner.TryCancel(request.JobId);
        return Ok(new CancelAutoWizardResponse(cancelled));
    }
}
