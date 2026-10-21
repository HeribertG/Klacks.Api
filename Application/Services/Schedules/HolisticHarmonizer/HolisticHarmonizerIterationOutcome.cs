// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// How one inner-loop iteration of <see cref="HolisticHarmonizerEngine"/> ended.
/// </summary>
internal enum HolisticHarmonizerIterationOutcome
{
    /// <summary>The iteration ran to its end and reports progress.</summary>
    Completed,

    /// <summary>The response was unusable; the loop continues without a progress report.</summary>
    Skipped,

    /// <summary>The loop ends: the LLM signalled satisfaction or too many unusable responses in a row.</summary>
    Stop,
}
