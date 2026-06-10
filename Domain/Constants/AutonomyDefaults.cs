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

    public const AutonomyLevel DefaultLevel = AutonomyLevel.Autonomous;

    public const AutonomyLevel MinimumLevel = AutonomyLevel.Propose;

    public const AutonomyLevel MaximumLevel = AutonomyLevel.FullyAutonomous;
}
