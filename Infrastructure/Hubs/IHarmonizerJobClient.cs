// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for harmonizer job progress streams.
/// </summary>
public interface IHarmonizerJobClient
{
    Task OnProgress(HarmonizerJobProgressDto progress);

    Task OnCompleted(HarmonizerJobResultDto result);

    Task OnCancelled();

    Task OnFailed(string reason);
}
