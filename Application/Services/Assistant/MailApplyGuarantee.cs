// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The mail-driven entry guarantee of SkillToolsetGuaranteeResolver: when the user asks to enter what the e-mails contain
/// (MailApplyIntentResolver), the person lookup, both planning-command writes, the availability write and the schedule view
/// are kept as hints. Retrieval cannot rank them on such a wording ("Trag das bitte alles ein"): the mail-management skills
/// win the pool, the toolset hit the provider cap without any write skill (live 2026-11-04) and the model answered that it
/// had no access to availabilities and keywords. A message that names the mails itself ("Lies die Mails und trag ... ein") also
/// keeps list_emails and read_email, so the mails can be opened in the same turn. Visibility only: the autonomy gate and the model's own choice stay untouched.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class MailApplyGuarantee
{
    /// <summary>Keeps the entry skills as hints when the turn is a mail-driven entry and says whether it is one.</summary>
    /// <param name="guaranteedSkills">The skills guaranteed so far; receives the entry skills</param>
    /// <param name="guaranteedSources">The source per guaranteed skill</param>
    /// <param name="request">The guarantee request with the message, the previous turn's skills and the permitted skills</param>
    internal static bool Apply(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        if (!MailApplyIntentResolver.IsMailApply(request.UserMessage, request.PreviousTurnSkillNames))
        {
            return false;
        }

        foreach (var skillName in MailApplyIntentResolver.GuaranteedSkillNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        if (MailApplyIntentResolver.NamesMails(request.UserMessage))
        {
            foreach (var skillName in MailApplyIntentResolver.MailReadSkillNames)
            {
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }

        return true;
    }
}
