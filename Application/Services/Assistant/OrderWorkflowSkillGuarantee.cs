// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The order-workflow guarantees of SkillToolsetGuaranteeResolver: an order is created (create_shift), sealed (seal_shift)
/// and then split into parts (cut_shift). Neither follow-up is always-on, and vector retrieval and the probabilistic
/// co-required expansion can miss them on the create turn and always miss them on the turn after it, whose message is a
/// bare "Ja" or a group name. So whenever create_shift is retrieved, or the immediately preceding turn ran it
/// (ContinuationSkillNames), seal_shift and cut_shift are kept as hints: the cut recipe confirms its held seal in that turn
/// and forces cut_shift afterwards (CutRecipeContinuationResolver). After a create_shift turn list_groups and
/// add_shift_to_group are kept too, at the lowest rank, because a seal refused for a missing group asks the user for the
/// group and the answer names neither skill. Visibility only: the autonomy gate and the confirmations are untouched.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class OrderWorkflowSkillGuarantee
{
    private const string ListGroupsSkillName = "list_groups";
    private const string AddShiftToGroupSkillName = "add_shift_to_group";

    /// <summary>Keeps seal_shift and cut_shift when create_shift is retrieved or ran in the preceding turn.</summary>
    /// <param name="guaranteedSkills">The skills guaranteed so far; receives the follow-ups</param>
    /// <param name="guaranteedSources">The source per guaranteed skill</param>
    /// <param name="permittedSkills">The skills the user may use</param>
    /// <param name="request">The guarantee request with the retrieved and the continuation skills</param>
    internal static void ApplyFollowUps(
        HashSet<AgentSkill> guaranteedSkills,
        Dictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        SkillToolsetGuaranteeRequest request)
    {
        if (!CreateShiftRetrieved(request) && !CreateShiftRanBefore(request))
        {
            return;
        }

        SkillGuaranteeCollector.Add(
            guaranteedSkills, permittedSkills, RecipeConstants.SealShiftSkill, ToolsetSkillSource.Hint, guaranteedSources);
        SkillGuaranteeCollector.Add(
            guaranteedSkills, permittedSkills, RecipeConstants.CutShiftSkill, ToolsetSkillSource.Hint, guaranteedSources);
    }

    /// <summary>Keeps list_groups and add_shift_to_group, lowest-ranked, on the turn after a create_shift.</summary>
    /// <param name="guaranteedSkills">The skills guaranteed so far; receives the group skills</param>
    /// <param name="guaranteedSources">The source per guaranteed skill</param>
    /// <param name="lowestRankedNames">The skills dropped first at truncation; receives the group skills claimed only here</param>
    /// <param name="request">The guarantee request with the continuation skills</param>
    internal static void ApplyGroupAnswer(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        HashSet<string> lowestRankedNames,
        SkillToolsetGuaranteeRequest request)
    {
        if (!CreateShiftRanBefore(request))
        {
            return;
        }

        SkillGuaranteeCollector.AddAsLowestRankedHint(
            guaranteedSkills, request.PermittedSkills, ListGroupsSkillName, guaranteedSources, lowestRankedNames);
        SkillGuaranteeCollector.AddAsLowestRankedHint(
            guaranteedSkills, request.PermittedSkills, AddShiftToGroupSkillName, guaranteedSources, lowestRankedNames);
    }

    private static bool CreateShiftRetrieved(SkillToolsetGuaranteeRequest request) =>
        request.RetrievedSkills.Any(skill =>
            string.Equals(skill.Name, RecipeConstants.CreateShiftSkill, StringComparison.OrdinalIgnoreCase));

    private static bool CreateShiftRanBefore(SkillToolsetGuaranteeRequest request) =>
        request.ContinuationSkillNames?.Contains(RecipeConstants.CreateShiftSkill, StringComparer.OrdinalIgnoreCase) == true;
}
