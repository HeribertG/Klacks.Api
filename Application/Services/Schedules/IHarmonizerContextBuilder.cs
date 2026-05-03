// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Reads the saved schedule (Work entities + agent metadata + shift preferences + Wizard 1
/// softening hints) and produces the BitmapInput consumed by the harmonizer.
/// </summary>
public interface IHarmonizerContextBuilder
{
    Task<BitmapInput> BuildContextAsync(HarmonizerContextRequest request, CancellationToken ct);
}
