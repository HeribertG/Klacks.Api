// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Result category of one vision-critic review at the end of a Wizard 3 run.
/// </summary>
public enum PlanCriticStatus
{
    /// <summary>The critic did not run (no model configured, or the model is known to be text-only).</summary>
    Skipped = 0,

    /// <summary>The critic answered; its findings were checked against the real plan.</summary>
    Completed = 1,

    /// <summary>The critic was configured but its call or its answer failed; the run result is unaffected.</summary>
    Failed = 2,
}
