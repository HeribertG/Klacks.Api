// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record UnattendedSkillDecision(bool Allowed, string? Reason)
{
    public static UnattendedSkillDecision Allow() => new(true, null);

    public static UnattendedSkillDecision Deny(string reason) => new(false, reason);
}
