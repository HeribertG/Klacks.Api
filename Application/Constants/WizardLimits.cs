// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Hard limits for a single Wizard 1 (planner GA) run. The GA scales with agents x shifts;
/// runs above these limits exhaust the 90s time budget without producing a usable plan.
/// These constants are enforced both client-side (precheck in the wizard dialog) and
/// server-side (controller) so direct API callers cannot bypass the gate.
/// </summary>
public static class WizardLimits
{
    /// <summary>Maximum agents (clients) per wizard run.</summary>
    public const int MaxAgents = 100;

    /// <summary>Maximum unique shifts per wizard run.</summary>
    public const int MaxShifts = 50;

    /// <summary>Machine code returned in the 400 response body when limits are exceeded.</summary>
    public const string TooLargeErrorCode = "WIZARD_TOO_LARGE";
}
