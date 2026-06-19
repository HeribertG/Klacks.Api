// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Coordinates background execution of harmonizer jobs and exposes cancellation/status queries.
/// </summary>
public interface IHarmonizerJobRunner
{
    Task<Guid> StartAsync(HarmonizerContextRequest request, CancellationToken externalCt);

    bool TryCancel(Guid jobId);

    bool IsRunning(Guid jobId);
}
