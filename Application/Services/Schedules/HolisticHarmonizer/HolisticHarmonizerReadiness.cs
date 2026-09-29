// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Whether a Holistic Harmonizer run can do its work right now. Ready means "worth starting": a model is
/// configured and it is not known to be unable to read the schedule image. A model whose vision capability
/// has not been measured yet counts as ready, because the run itself performs that measurement.
/// </summary>
/// <param name="IsReady">True when a run is worth starting.</param>
/// <param name="Reason">Why a run is not worth starting; null when ready.</param>
public sealed record HolisticHarmonizerReadiness(bool IsReady, string? Reason)
{
    public static HolisticHarmonizerReadiness Ready() => new(true, null);

    public static HolisticHarmonizerReadiness NotReady(string reason) => new(false, reason);
}
