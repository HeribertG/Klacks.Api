// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class AgentSkillDefaults
{
    public const string Category = "backend";
    public const string HandlerType = "internal";
    public const int SkillNameMaxLength = 256;

    // Fail-closed: an unclassified skill is treated as the most consequential effect, mirroring
    // SkillRiskClassifier's own default-to-Irreversible fallback for anything it cannot place.
    public const SkillEffect Effect = SkillEffect.Mutate;
}
