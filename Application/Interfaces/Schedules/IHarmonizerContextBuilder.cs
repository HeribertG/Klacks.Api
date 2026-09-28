// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Reads the saved schedule (Work entities + agent metadata + shift preferences + Wizard 1
/// softening hints) and produces the BitmapInput consumed by the harmonizer.
/// </summary>
public interface IHarmonizerContextBuilder
{
    Task<BitmapInput> BuildContextAsync(HarmonizerContextRequest request, CancellationToken ct);
}
