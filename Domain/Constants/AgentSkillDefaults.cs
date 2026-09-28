// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Seed version of a row the seed loader has not written under the seed-version rule yet. The migration
    /// that introduced agent_skills.seed_version gives every existing row this value, which is what makes the
    /// loader adopt each row exactly once after that release.
    /// </summary>
    public const int UnseededVersion = 0;
}
