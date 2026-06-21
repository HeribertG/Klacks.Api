// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defaults and shared constants for the per-user autonomy level gating of skill execution.
/// DefaultLevel preserves the pre-gating behavior (everything except sensitive skills executes
/// without confirmation); ConfirmationTokenParameter is the reserved skill parameter used to
/// replay a gated invocation after the user confirmed.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class AutonomyDefaults
{
    public const string ConfirmationTokenParameter = "confirmation_token";

    public const string ConfirmPendingActionSkillName = "confirm_pending_action";

    public const int ConfirmationTtlMinutes = 5;

    // A confirmation may be replayed explicitly (via its token) for the full TTL above, but the
    // orchestrator only AUTO-FORCES a tool call on an affirmation ("ja") when the pending action is
    // fresh — i.e. the affirmation immediately follows the confirmation request. This bounds the
    // window in which a stale or misdirected "ja" could fire a forgotten (irreversible) pending action.
    public const int ConfirmationForceWindowSeconds = 120;

    public const AutonomyLevel DefaultLevel = AutonomyLevel.Autonomous;

    public const AutonomyLevel MinimumLevel = AutonomyLevel.Propose;

    public const AutonomyLevel MaximumLevel = AutonomyLevel.FullyAutonomous;
}
