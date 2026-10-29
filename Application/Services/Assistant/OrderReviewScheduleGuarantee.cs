// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The schedule follow-up of the order review for SkillToolsetGuaranteeResolver: a confirmed execute_order_review offers
/// to show the new plannable shifts in the schedule ("only on a yes call open_schedule"), and the user's answer is a bare
/// yes ("Oui, montre-les-moi dans le planning.") that retrieval cannot rank open_schedule on; the toolset then hit the
/// provider cap without it and the model fell back to navigate_to, which opens the schedule without group and week
/// (found 2026-10-29 in the fr and it story takes). So when the immediately preceding turn ran execute_order_review
/// (ContinuationSkillNames, a confirmed held action named by the skill it replayed) and the reply opens with an
/// affirmation, open_schedule is kept as a hint. Visibility only: open_schedule is a read-only navigation and the model
/// still decides whether to call it. Accepted trade-off: the guarantee cannot tell which question the yes answers, so a
/// yes to the next review question after a partial execute (the review stays open) keeps open_schedule as a hint too;
/// the review's own loop skills stay guaranteed beside it.
/// </summary>

using Klacks.Api.Application.Skills;
using Klacks.Api.Application.Skills.OrderReview;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class OrderReviewScheduleGuarantee
{
    /// <summary>Keeps open_schedule on an affirmative reply right after an execute_order_review turn.</summary>
    /// <param name="guaranteedSkills">The skills guaranteed so far; receives open_schedule</param>
    /// <param name="guaranteedSources">The source per guaranteed skill</param>
    /// <param name="request">The guarantee request with the message, its language and the continuation skills</param>
    internal static void Apply(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        var executedBefore = request.ContinuationSkillNames?.Contains(
            ExecuteOrderReviewSkill.SkillName, StringComparer.OrdinalIgnoreCase) == true;
        if (!executedBefore || !AffirmationDetector.LeadsWithAffirmation(request.UserMessage, request.Language))
        {
            return;
        }

        SkillGuaranteeCollector.Add(
            guaranteedSkills, request.PermittedSkills, OpenScheduleSkill.SkillName, ToolsetSkillSource.Hint, guaranteedSources);
    }
}
