// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Response of the harmonizer job status endpoint. Lets clients that missed SignalR events
/// (reconnect window) recover the job outcome.
/// </summary>
/// <param name="Status">One of the WizardJobStatusValues constants.</param>
/// <param name="Result">Final result when the job completed, otherwise null.</param>
/// <param name="Reason">Failure reason when the job failed, otherwise null.</param>
public sealed record HarmonizerJobStatusResponse(
    string Status,
    HarmonizerJobResultDto? Result,
    string? Reason);
