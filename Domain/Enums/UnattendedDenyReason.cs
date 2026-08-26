// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Machine-readable cause of an UnattendedSkillDecision refusal, so a caller can react per cause
/// instead of parsing the human-readable reason text. IrreversibleWithoutOptIn is the only cause a
/// scheduled task can recover from without being recreated, which is why it is separated out.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum UnattendedDenyReason
{
    None = 0,
    NoPermissions = 1,
    UnknownSkill = 2,
    SensitiveSkill = 3,
    AutonomyLevelTooLow = 4,
    IrreversibleWithoutOptIn = 5,
    UnknownRiskClass = 6
}
