// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a replay may ask the model a second time after its first tool choice. Production
/// runs a multi-iteration loop, so a read-only lookup or an advisory/evaluation skill in front of a
/// mutation is a legitimate first step there; a single-call replay would book it as a selection miss.
/// Navigation and UI passthrough skills are excluded: they end or redirect the turn in production and
/// remain real wrong choices.
/// </summary>
/// <param name="chosen">Skill the model picked in the first step, null when it is not a known skill</param>
/// <param name="expected">Skill the goldset item expects, null when it is not a known skill</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class ReplayFollowUpPolicy
{
    private static readonly HashSet<string> NavigationSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        SkillNames.NavigateTo,
        SkillNames.SearchAndNavigate
    };

    public static bool ShouldFollowUp(AgentSkill? chosen, AgentSkill? expected) =>
        chosen != null
        && expected != null
        && (chosen.Effect == SkillEffect.Read || chosen.Effect == SkillEffect.Advise)
        && expected.Effect == SkillEffect.Mutate
        && string.Equals(chosen.ExecutionType, LlmExecutionTypes.Skill, StringComparison.Ordinal)
        && !NavigationSkills.Contains(chosen.Name);
}
