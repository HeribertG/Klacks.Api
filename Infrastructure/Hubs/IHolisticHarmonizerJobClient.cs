// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client contract for Holistic Harmonizer job progress streams.
/// </summary>
public interface IHolisticHarmonizerJobClient
{
    Task OnProgress(HolisticHarmonizerJobProgressDto progress);

    Task OnCompleted(HolisticHarmonizerRunResponse result);

    Task OnCancelled();

    Task OnFailed(string reason);
}
