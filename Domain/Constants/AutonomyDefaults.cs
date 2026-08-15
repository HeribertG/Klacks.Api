// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defaults and shared constants for the per-user autonomy level gating of skill execution.
/// At DefaultLevel (Autonomous) only Sensitive skills are held for confirmation - see
/// AutonomyGateService.IsAllowed, where Sensitive is held at every level while Irreversible passes
/// from Autonomous upwards. Lowering the level to Assisted or Propose holds the remaining classes too.
/// ConfirmationTokenParameter is the reserved skill parameter used to replay a gated invocation
/// after the user confirmed.
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

    // A pending proposal hint only makes the paired apply_* skill VISIBLE in the tool set; it never
    // executes anything (the apply call still runs through the autonomy gate). It therefore gets a longer
    // window than the force above: the reply to a proposal is often not a bare "ja" but the answer to a
    // follow-up question the model asked ("from which date?"), which the user needs time to decide.
    // This equals ConfirmationTtlMinutes on purpose — it is the LONGEST window the store can express.
    // PeekLatestForUser reconstructs a row's age as (now - (ExpiresAtUtc - ConfirmationTtlMinutes)) and
    // only sees rows that have not expired yet, so a larger value here would silently have no effect.
    public const int ProposalHintWindowSeconds = ConfirmationTtlMinutes * 60;

    public const AutonomyLevel DefaultLevel = AutonomyLevel.Autonomous;

    public const AutonomyLevel MinimumLevel = AutonomyLevel.Propose;

    public const AutonomyLevel MaximumLevel = AutonomyLevel.FullyAutonomous;
}
