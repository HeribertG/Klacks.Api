// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Builds the API-facing <see cref="HolisticHarmonizerRunResponse"/> from a raw engine result.
/// Shared between the synchronous controller path (legacy) and the SignalR-based JobRunner so
/// both surfaces produce identical wire payloads.
/// </summary>
public static class HolisticHarmonizerResponseMapper
{
    private const int RoundingDigits = 4;

    public static HolisticHarmonizerRunResponse ToResponse(Guid jobId, HolisticHarmonizerRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

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
                Math.Round(i.ScoreBefore, RoundingDigits, MidpointRounding.AwayFromZero),
                Math.Round(i.ScoreAfter, RoundingDigits, MidpointRounding.AwayFromZero),
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

        var agentDisplayNames = result.OriginalBitmap.Rows
            .Select(r => r.DisplayName)
            .ToArray();

        return new HolisticHarmonizerRunResponse(
            JobId: jobId,
            LlmModelId: result.LlmModelId,
            FitnessBefore: Math.Round(result.FitnessBefore, RoundingDigits, MidpointRounding.AwayFromZero),
            FitnessAfter: Math.Round(result.FitnessAfter, RoundingDigits, MidpointRounding.AwayFromZero),
            AcceptedSwaps: accepted,
            RejectedSwaps: rejected,
            Batches: batches,
            AgentDisplayNames: agentDisplayNames,
            LlmParsingError: result.LlmParsingError,
            LlmRawResponsePreview: result.LlmRawResponsePreview);
    }
}
