// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The schedule-overlay guarantee of SkillToolsetGuaranteeResolver: a message that asks to see the schedule with the
/// availabilities or the planning commands shown (ScheduleOverlayIntentResolver) keeps open_schedule, the only skill with the
/// showAvailability and showCommands switches, as a hint. Visibility only: open_schedule is a read-only navigation and the
/// model still decides whether to call it.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class ScheduleOverlayGuarantee
{
    /// <summary>Keeps open_schedule when the message asks for the schedule with the availabilities or keywords.</summary>
    /// <param name="guaranteedSkills">The skills guaranteed so far; receives open_schedule</param>
    /// <param name="guaranteedSources">The source per guaranteed skill</param>
    /// <param name="request">The guarantee request with the message and the permitted skills</param>
    internal static void Apply(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        if (!ScheduleOverlayIntentResolver.IsOverlayRequest(request.UserMessage))
        {
            return;
        }

        SkillGuaranteeCollector.Add(
            guaranteedSkills, request.PermittedSkills, ScheduleOverlayIntentResolver.OpenScheduleSkillName,
            ToolsetSkillSource.Hint, guaranteedSources);
    }
}
