// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;

/// <summary>
/// Tells whether the Holistic Harmonizer (Wizard 3) prerequisites are met: a configured model that is not
/// known to be text-only. Cheap by design - it only reads the setting and the cached capability verdict and
/// never calls the model, so the AutoWizard chain can ask it before and after its third stage.
/// </summary>
public interface IHolisticHarmonizerReadinessCheck
{
    Task<HolisticHarmonizerReadiness> CheckAsync(CancellationToken cancellationToken = default);
}
