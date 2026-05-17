// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Hard limits and warning thresholds for the AutoWizard orchestrator. The chain
/// (Wizard 1 + Harmonizer + Holistic Harmonizer) scales with agents x shifts x periodDays;
/// runs above these limits will exhaust the 90s Wizard 1 time budget and produce no
/// scenario. These constants are enforced both client-side (precheck) and server-side
/// (controller) so direct API callers cannot bypass the gate.
/// </summary>
public static class AutoWizardLimits
{
    /// <summary>Maximum agents (clients) per AutoWizard run.</summary>
    public const int MaxAgents = 250;

    /// <summary>Maximum unique shifts per AutoWizard run.</summary>
    public const int MaxShifts = 80;

    /// <summary>Maximum (agents x shifts x periodDays) decision space.</summary>
    public const int MaxSlotProduct = 25_000;

    /// <summary>Agent count above which the UI should warn before starting.</summary>
    public const int WarnAgents = 120;

    /// <summary>Machine code returned in the 400 response body when limits are exceeded.</summary>
    public const string TooLargeErrorCode = "AUTO_WIZARD_TOO_LARGE";
}
