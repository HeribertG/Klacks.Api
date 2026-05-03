// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for the schedule harmonizer (Wizard 2). Start launches a background
/// optimisation job and returns a job id; progress streams via the harmonizer SignalR hub.
/// ApplyAsScenario materialises the cached harmonised bitmap as Work entities in a new
/// AnalyseScenario so the source schedule remains untouched.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/backend/[controller]")]
public sealed class HarmonizerController : ControllerBase
{
    private readonly IHarmonizerJobRunner _runner;
    private readonly IHarmonizerApplyService _applyService;

    public HarmonizerController(IHarmonizerJobRunner runner, IHarmonizerApplyService applyService)
    {
        _runner = runner;
        _applyService = applyService;
    }

    [HttpPost("Start")]
    public async Task<ActionResult<StartHarmonizerResponse>> Start(
        [FromBody] StartHarmonizerRequest request,
        CancellationToken ct)
    {
        var jobId = await _runner.StartAsync(
            new HarmonizerContextRequest(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: request.AnalyseToken),
            ct);

        return Ok(new StartHarmonizerResponse(jobId));
    }

    [HttpPost("Cancel")]
    public ActionResult<CancelHarmonizerResponse> Cancel([FromBody] CancelHarmonizerRequest request)
    {
        var cancelled = _runner.TryCancel(request.JobId);
        return Ok(new CancelHarmonizerResponse(cancelled));
    }

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyHarmonizerAsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyHarmonizerAsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, createdIds) = await _applyService.ApplyAsScenarioAsync(request.JobId, request.GroupId, ct);
            return Ok(new ApplyHarmonizerAsScenarioResponse(scenario.Id, scenario.Token, scenario.Name, createdIds));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario</param>
public sealed record StartHarmonizerRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken);

public sealed record StartHarmonizerResponse(Guid JobId);

public sealed record CancelHarmonizerRequest(Guid JobId);

public sealed record CancelHarmonizerResponse(bool Cancelled);

/// <param name="JobId">The harmonizer job whose cached result is materialised</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness</param>
public sealed record ApplyHarmonizerAsScenarioRequest(Guid JobId, Guid? GroupId);

public sealed record ApplyHarmonizerAsScenarioResponse(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    IReadOnlyList<Guid> CreatedWorkIds);
