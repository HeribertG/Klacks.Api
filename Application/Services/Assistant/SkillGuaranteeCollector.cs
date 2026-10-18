// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Collects the skills one turn guarantees, for SkillToolsetGuaranteeResolver: a permitted skill is added once, with the
/// strongest source any layer claimed for it, and a skill that only a weak guarantee claims can be marked lowest-ranked,
/// so it is the first of its tier to go at a small provider cap.
/// </summary>
/// <param name="guaranteedSkills">Set the resolved skills are added to</param>
/// <param name="permittedSkills">Skills the user may use, and the instances the truncation compares against</param>
/// <param name="guaranteedSources">Source per guaranteed skill name; the strongest source wins</param>
/// <param name="lowestRankedNames">Guaranteed skills that rank last within their source at truncation</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class SkillGuaranteeCollector
{
    public static void Add(
        HashSet<AgentSkill> guaranteedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        string skillName,
        ToolsetSkillSource source,
        IDictionary<string, ToolsetSkillSource> guaranteedSources)
    {
        var skill = permittedSkills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill == null)
        {
            return;
        }

        // A skill can be guaranteed by several layers at once (keyword + learned phrase + recipe).
        // One source per candidate keeps the W5 provenance distribution additive, so the strongest
        // deterministic reason wins: RecipeStep > LearnedPhrase > Keyword > Hint.
        if (guaranteedSources.TryGetValue(skill.Name, out var existing) &&
            existing.Priority() >= source.Priority())
        {
            guaranteedSkills.Add(skill);
            return;
        }

        guaranteedSources[skill.Name] = source;
        guaranteedSkills.Add(skill);
    }

    /// <summary>
    /// Adds the skill as a Hint and marks it lowest-ranked, unless another layer already claimed it: a skill that another
    /// guarantee or the same message's keywords asked for keeps its ordinary rank.
    /// </summary>
    /// <param name="skillName">Skill guaranteed only as a convenience next to the skills the turn really needs</param>
    public static void AddAsLowestRankedHint(
        HashSet<AgentSkill> guaranteedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        string skillName,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        HashSet<string> lowestRankedNames)
    {
        var claimedElsewhere = guaranteedSources.ContainsKey(skillName);
        Add(guaranteedSkills, permittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);

        if (!claimedElsewhere && guaranteedSources.ContainsKey(skillName))
        {
            lowestRankedNames.Add(skillName);
        }
    }
}
