// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// REST entry point for Holistic Harmonizer (LLM-driven schedule harmonizer). The MVP runs a single
/// synchronous round-trip to the configured LLM, validates each proposed swap against the
/// hard constraints, and stores the resulting bitmap in the shared harmonizer result cache.
/// Apply uses the existing <see cref="IHarmonizerApplyService"/> so the scenario flow is
/// identical to Wizard 2.
/// </summary>
[ApiController]
public sealed class HolisticHarmonizerController : BaseController
{
    private readonly HolisticHarmonizerRunService _runService;
    private readonly IHolisticHarmonizerApplyService _applyService;
    private readonly HolisticHarmonizerModelCheckService _modelCheckService;

    public HolisticHarmonizerController(
        HolisticHarmonizerRunService runService,
        IHolisticHarmonizerApplyService applyService,
        HolisticHarmonizerModelCheckService modelCheckService)
    {
        _runService = runService;
        _applyService = applyService;
        _modelCheckService = modelCheckService;
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
                r.Error))
            .ToArray();
        return Ok(new HolisticHarmonizerModelCheckResponse(dtos));
    }

    [HttpPost("Run")]
    public async Task<ActionResult<HolisticHarmonizerRunResponse>> Run([FromBody] HolisticHarmonizerRunRequest request, CancellationToken ct)
    {
        var outcome = await _runService.RunAsync(
            new HolisticHarmonizerRunInput(
                PeriodFrom: request.PeriodFrom,
                PeriodUntil: request.PeriodUntil,
                AgentIds: request.AgentIds,
                AnalyseToken: request.AnalyseToken,
                Language: request.Language),
            ct);

        if (!outcome.IsSuccess || outcome.Result is null || outcome.JobId is null)
        {
            return UnprocessableEntity(new { message = outcome.FailureMessage ?? "Holistic Harmonizer run failed." });
        }

        return Ok(BuildResponse(outcome.JobId.Value, outcome.Result));
    }

    [HttpPost("ApplyAsScenario")]
    public async Task<ActionResult<ApplyHolisticHarmonizerAsScenarioResponse>> ApplyAsScenario(
        [FromBody] ApplyHolisticHarmonizerAsScenarioRequest request,
        CancellationToken ct)
    {
        try
        {
            var (scenario, createdIds) = await _applyService.ApplyAsScenarioAsync(request.JobId, request.GroupId, ct);
            return Ok(new ApplyHolisticHarmonizerAsScenarioResponse(scenario.Id, scenario.Token, scenario.Name, scenario.RunGroupId, createdIds));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private static HolisticHarmonizerRunResponse BuildResponse(Guid jobId, HolisticHarmonizerRunResult result)
    {
        var accepted = result.Iterations
            .SelectMany(i => i.AppliedSteps)
            .Select(s => new HolisticHarmonizerSwapDto(s.RowA, s.DayA, s.RowB, s.DayB, s.Reason))
            .ToArray();
        var rejected = result.Iterations
            .SelectMany(i => i.Rejections)
            .Select(r => new HolisticHarmonizerRejectionDto(
                new HolisticHarmonizerSwapDto(r.Swap.RowA, r.Swap.DayA, r.Swap.RowB, r.Swap.DayB, r.Swap.Reason),
                r.Reason.ToString(),
                r.Detail))
            .ToArray();
        var batches = result.Iterations
            .Select(i => new HolisticHarmonizerBatchDto(
                i.BatchId,
                i.Intent,
                i.Result.ToString(),
                i.AppliedSteps.Count,
                i.Rejections.Count,
                i.StoppedAtStep,
                Math.Round(i.ScoreBefore, 4, MidpointRounding.AwayFromZero),
                Math.Round(i.ScoreAfter, 4, MidpointRounding.AwayFromZero),
                i.AppliedSteps
                    .Select(s => new HolisticHarmonizerSwapDto(s.RowA, s.DayA, s.RowB, s.DayB, s.Reason))
                    .ToArray(),
                i.Rejections
                    .Select(r => new HolisticHarmonizerRejectionDto(
                        new HolisticHarmonizerSwapDto(r.Swap.RowA, r.Swap.DayA, r.Swap.RowB, r.Swap.DayB, r.Swap.Reason),
                        r.Reason.ToString(),
                        r.Detail))
                    .ToArray()))
            .ToArray();

        return new HolisticHarmonizerRunResponse(
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

