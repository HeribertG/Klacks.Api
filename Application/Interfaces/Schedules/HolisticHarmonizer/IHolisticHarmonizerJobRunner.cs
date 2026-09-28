// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;

/// <summary>
/// Coordinates background execution of Holistic Harmonizer jobs and exposes cancellation/status queries.
/// </summary>
public interface IHolisticHarmonizerJobRunner
{
    Task<Guid> StartAsync(HolisticHarmonizerRunInput input, CancellationToken chainCt);

    bool TryCancel(Guid jobId);

    bool IsRunning(Guid jobId);
}
