// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a skill may run unattended on a background path, where nobody can confirm anything
/// and the autonomy gate is bypassed. It fails closed: without frozen owner permissions there is no
/// permission check at all, an unknown skill cannot be classified, and a skill that is sensitive
/// <em>today</em> is refused even when it was harmless at the time the schedule was created.
/// </summary>
/// <param name="registry">Resolves the skill name to its descriptor</param>
/// <param name="classifier">Yields the current risk class of that descriptor</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public sealed class UnattendedSkillPolicy : IUnattendedSkillPolicy
{
    private readonly ISkillRegistry _registry;
    private readonly ISkillRiskClassifier _classifier;

    public UnattendedSkillPolicy(ISkillRegistry registry, ISkillRiskClassifier classifier)
    {
        _registry = registry;
        _classifier = classifier;
    }

    public UnattendedSkillDecision Decide(string skillName, IReadOnlyList<string> ownerPermissions)
    {
        if (ownerPermissions.Count == 0)
        {
            return UnattendedSkillDecision.Deny(
                "The owner permissions of this task were never frozen, so a background run would have " +
                "no permission check at all. The task was disabled; please create it again.");
        }

        var descriptor = _registry.GetSkillByName(skillName);
        if (descriptor is null)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{skillName}' no longer exists. The task was disabled.");
        }

        if (_classifier.Classify(descriptor) == SkillRiskClass.Sensitive)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{skillName}' is now classified as sensitive and must not run unattended. " +
                "The task was disabled; run the skill interactively instead.");
        }

        return UnattendedSkillDecision.Allow();
    }
}
