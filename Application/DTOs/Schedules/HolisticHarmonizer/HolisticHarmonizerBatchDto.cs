// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="BatchId">Stable id correlating to engine logs.</param>
/// <param name="Intent">LLM-supplied intent label (e.g. "consolidate_block").</param>
/// <param name="Result">Acceptance category: Accepted, PartiallyAccepted, Rejected, WouldDegrade.</param>
/// <param name="AppliedStepCount">How many steps survived hard constraints and Score-Greedy.</param>
/// <param name="RejectionCount">How many steps failed hard constraints inside this batch.</param>
/// <param name="StoppedAtStep">Zero-based index of the first failing step, null when all steps passed.</param>
/// <param name="ScoreBefore">Bitmap fitness when the batch started.</param>
/// <param name="ScoreAfter">Bitmap fitness after the batch was committed (or reverted).</param>
/// <param name="AppliedSwaps">Swaps that were ultimately committed to the bitmap (empty when Rejected/WouldDegrade).</param>
/// <param name="Rejections">Per-step rejection records inside this batch (empty when fully accepted).</param>
public sealed record HolisticHarmonizerBatchDto(
    Guid BatchId,
    string Intent,
    string Result,
    int AppliedStepCount,
    int RejectionCount,
    int? StoppedAtStep,
    double ScoreBefore,
    double ScoreAfter,
    IReadOnlyList<HolisticHarmonizerSwapDto> AppliedSwaps,
    IReadOnlyList<HolisticHarmonizerRejectionDto> Rejections);
