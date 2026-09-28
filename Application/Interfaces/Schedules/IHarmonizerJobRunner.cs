// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Coordinates background execution of harmonizer jobs and exposes cancellation/status queries.
/// </summary>
public interface IHarmonizerJobRunner
{
    Task<Guid> StartAsync(HarmonizerContextRequest request, CancellationToken chainCt);

    bool TryCancel(Guid jobId);

    bool IsRunning(Guid jobId);
}
