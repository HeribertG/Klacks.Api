// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of the unattended skill policy. Reason carries the human-readable text delivered to the
/// owner and written to logs; DenyReason carries the same verdict machine-readably so a caller can
/// react per cause without parsing that text.
/// </summary>
/// <param name="Allowed">True when the background run may proceed.</param>
/// <param name="Reason">Human-readable refusal text; null when allowed.</param>
/// <param name="DenyReason">Machine-readable refusal cause; None when allowed.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record UnattendedSkillDecision(bool Allowed, string? Reason, UnattendedDenyReason DenyReason)
{
    public static UnattendedSkillDecision Allow() => new(true, null, UnattendedDenyReason.None);

    public static UnattendedSkillDecision Deny(string reason, UnattendedDenyReason denyReason) =>
        new(false, reason, denyReason);
}
