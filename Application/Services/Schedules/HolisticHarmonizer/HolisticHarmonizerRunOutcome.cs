// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Outcome of <see cref="HolisticHarmonizerRunService.RunAsync"/>: either a successful run with cached
/// result and job id (ready to apply as a scenario), or a failure message for the UI.
/// </summary>
public sealed record HolisticHarmonizerRunOutcome
{
    public bool IsSuccess { get; init; }
    public Guid? JobId { get; init; }
    public HolisticHarmonizerRunResult? Result { get; init; }
    public string? FailureMessage { get; init; }

    public static HolisticHarmonizerRunOutcome Success(Guid jobId, HolisticHarmonizerRunResult result)
        => new() { IsSuccess = true, JobId = jobId, Result = result };

    public static HolisticHarmonizerRunOutcome Failure(string message)
        => new() { IsSuccess = false, FailureMessage = message };
}
