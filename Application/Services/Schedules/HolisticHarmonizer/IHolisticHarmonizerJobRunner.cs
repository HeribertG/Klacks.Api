// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Coordinates background execution of Holistic Harmonizer jobs and exposes cancellation/status queries.
/// </summary>
public interface IHolisticHarmonizerJobRunner
{
    Task<Guid> StartAsync(HolisticHarmonizerRunInput input, CancellationToken externalCt);

    bool TryCancel(Guid jobId);

    bool IsRunning(Guid jobId);
}
