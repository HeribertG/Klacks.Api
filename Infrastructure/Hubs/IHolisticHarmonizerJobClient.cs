// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for Holistic Harmonizer job progress streams.
/// </summary>
public interface IHolisticHarmonizerJobClient
{
    Task OnProgress(HolisticHarmonizerJobProgressDto progress);

    Task OnCompleted(HolisticHarmonizerRunResponse result);

    Task OnCancelled();

    Task OnFailed(string reason);
}

/// <param name="JobId">Unique job identifier.</param>
/// <param name="IterationIndex">Index of the just-completed inner-loop iteration (0 = pre-flight tick).</param>
/// <param name="MaxIterations">Configured upper bound for the inner loop.</param>
/// <param name="BestFitness">Best fitness observed so far in [0,1].</param>
/// <param name="AcceptedBatchCount">Total accepted/partially-accepted batches so far.</param>
/// <param name="RejectedBatchCount">Total rejected/would-degrade batches so far.</param>
/// <param name="ElapsedMs">Wall-clock time since run start in milliseconds.</param>
public sealed record HolisticHarmonizerJobProgressDto(
    Guid JobId,
    int IterationIndex,
    int MaxIterations,
    double BestFitness,
    int AcceptedBatchCount,
    int RejectedBatchCount,
    long ElapsedMs);
