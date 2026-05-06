// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules.Wizard3;
using Klacks.ScheduleOptimizer.Wizard3.Mutations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for Wizard 3 (LLM-driven schedule harmonizer). The MVP runs a single
/// synchronous round-trip to the configured LLM, validates each proposed swap against the
/// hard constraints, and stores the resulting bitmap in the shared harmonizer result cache.
/// Apply uses the existing <see cref="IHarmonizerApplyService"/> so the scenario flow is
/// identical to Wizard 2.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/backend/[controller]")]
public sealed class Wizard3Controller : ControllerBase
{
    private readonly Wizard3RunService _runService;
    private readonly IWizard3ApplyService _applyService;
    private readonly Wizard3ModelCheckService _modelCheckService;

    public Wizard3Controller(
        Wizard3RunService runService,
        IWizard3ApplyService applyService,
        Wizard3ModelCheckService modelCheckService)
    {
        _runService = runService;
        _applyService = applyService;
        _modelCheckService = modelCheckService;
    }

    [HttpPost("CheckAllModels")]
    public async Task<ActionResult<Wizard3ModelCheckResponse>> CheckAllModels(CancellationToken ct)
    {
        var results = await _modelCheckService.CheckAllAsync(ct);
        var dtos = results
            .Select(r => new Wizard3ModelCheckDto(
                r.ModelId,
                r.DisplayName,
                r.ProviderId,
                r.IsHealthy,
                r.LatencyMs,
                r.Error))
            .ToArray();
        return Ok(new Wizard3ModelCheckResponse(dtos));
    }

    [HttpPost("Run")]
    public async Task<ActionResult<Wizard3RunResponse>> Run([FromBody] Wizard3RunRequest request, CancellationToken ct)
    {
        var outcome = await _runService.RunAsync(
            new Wizard3RunInput(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: request.AnalyseToken,
                Language: request.Language),
            ct);

        if (!outcome.IsSuccess || outcome.Result is null || outcome.JobId is null)
        {
            return UnprocessableEntity(new { message = outcome.FailureMessage ?? "Wizard 3 run failed." });
        }

        return Ok(BuildResponse(outcome.JobId.Value, outcome.Result));
    }

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyWizard3AsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyWizard3AsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, createdIds) = await _applyService.ApplyAsScenarioAsync(request.JobId, request.GroupId, ct);
            return Ok(new ApplyWizard3AsScenarioResponse(scenario.Id, scenario.Token, scenario.Name, scenario.RunGroupId, createdIds));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static Wizard3RunResponse BuildResponse(Guid jobId, Wizard3RunResult result)
    {
        var accepted = result.Iterations
            .SelectMany(i => i.AppliedSteps)
            .Select(s => new Wizard3SwapDto(s.RowA, s.DayA, s.RowB, s.DayB, s.Reason))
            .ToArray();
        var rejected = result.Iterations
            .SelectMany(i => i.Rejections)
            .Select(r => new Wizard3RejectionDto(
                new Wizard3SwapDto(r.Swap.RowA, r.Swap.DayA, r.Swap.RowB, r.Swap.DayB, r.Swap.Reason),
                r.Reason.ToString(),
                r.Detail))
            .ToArray();
        var batches = result.Iterations
            .Select(i => new Wizard3BatchDto(
                i.BatchId,
                i.Intent,
                i.Result.ToString(),
                i.AppliedSteps.Count,
                i.Rejections.Count,
                i.StoppedAtStep,
                Math.Round(i.ScoreBefore, 4, MidpointRounding.AwayFromZero),
                Math.Round(i.ScoreAfter, 4, MidpointRounding.AwayFromZero)))
            .ToArray();

        return new Wizard3RunResponse(
            JobId: jobId,
            LlmModelId: result.LlmModelId,
            FitnessBefore: Math.Round(result.FitnessBefore, 4, MidpointRounding.AwayFromZero),
            FitnessAfter: Math.Round(result.FitnessAfter, 4, MidpointRounding.AwayFromZero),
            AcceptedSwaps: accepted,
            RejectedSwaps: rejected,
            Batches: batches,
            LlmParsingError: result.LlmParsingError,
            LlmRawResponsePreview: result.LlmRawResponsePreview);
    }
}

/// <param name="PeriodFrom">Start date (inclusive).</param>
/// <param name="PeriodUntil">End date (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario.</param>
/// <param name="Language">UI language; the LLM writes its swap reasons in this locale.</param>
public sealed record Wizard3RunRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language);

public sealed record Wizard3SwapDto(int RowA, int DayA, int RowB, int DayB, string Reason);

public sealed record Wizard3RejectionDto(Wizard3SwapDto Swap, string Reason, string Detail);

public sealed record Wizard3RunResponse(
    Guid JobId,
    string LlmModelId,
    double FitnessBefore,
    double FitnessAfter,
    IReadOnlyList<Wizard3SwapDto> AcceptedSwaps,
    IReadOnlyList<Wizard3RejectionDto> RejectedSwaps,
    IReadOnlyList<Wizard3BatchDto> Batches,
    string? LlmParsingError,
    string? LlmRawResponsePreview);

/// <param name="BatchId">Stable id correlating to engine logs.</param>
/// <param name="Intent">LLM-supplied intent label (e.g. "consolidate_block").</param>
/// <param name="Result">Acceptance category: Accepted, PartiallyAccepted, Rejected, WouldDegrade.</param>
/// <param name="AppliedStepCount">How many steps survived hard constraints and Score-Greedy.</param>
/// <param name="RejectionCount">How many steps failed hard constraints inside this batch.</param>
/// <param name="StoppedAtStep">Zero-based index of the first failing step, null when all steps passed.</param>
/// <param name="ScoreBefore">Bitmap fitness when the batch started.</param>
/// <param name="ScoreAfter">Bitmap fitness after the batch was committed (or reverted).</param>
public sealed record Wizard3BatchDto(
    Guid BatchId,
    string Intent,
    string Result,
    int AppliedStepCount,
    int RejectionCount,
    int? StoppedAtStep,
    double ScoreBefore,
    double ScoreAfter);

/// <param name="JobId">The Wizard 3 run whose cached result is materialised.</param>
/// <param name="GroupId">Optional group scope for scenario cloning and name uniqueness.</param>
public sealed record ApplyWizard3AsScenarioRequest(Guid JobId, Guid? GroupId);

public sealed record ApplyWizard3AsScenarioResponse(
    Guid ScenarioId,
    Guid ScenarioToken,
    string ScenarioName,
    Guid? RunGroupId,
    IReadOnlyList<Guid> CreatedWorkIds);

public sealed record Wizard3ModelCheckDto(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    long LatencyMs,
    string? Error);

public sealed record Wizard3ModelCheckResponse(IReadOnlyList<Wizard3ModelCheckDto> Models);
