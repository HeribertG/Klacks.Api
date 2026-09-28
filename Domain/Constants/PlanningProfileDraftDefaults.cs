// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defaults for the planning-profile setup dialog: the time-of-day format accepted for the night-window
/// parameters, the lifetime after which an abandoned draft is pruned from the pending store, and the
/// fallback name given to the single rule created when the profile starts from scratch.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class PlanningProfileDraftDefaults
{
    public const int PendingDraftTtlMinutes = 60;

    public const string TimeOfDayFormat = "HH:mm";

    public const string DefaultScratchRuleName = "Custom planning profile";
}
