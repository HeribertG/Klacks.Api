// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cancels the in-progress company-rule intake by clearing the pending draft. No-op friendly: reports
/// when there was no draft to cancel. Draft-only; nothing is persisted.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("cancel_company_rule")]
public class CancelCompanyRuleSkill : BaseSkillImplementation
{
    private readonly IPendingCompanyRuleDraftStore _draftStore;

    public CancelCompanyRuleSkill(IPendingCompanyRuleDraftStore draftStore)
    {
        _draftStore = draftStore;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = CompanyRuleDraftScope.ConversationKey(context);
        var hadDraft = _draftStore.Get(context.UserId, conversationKey) is not null;
        _draftStore.Clear(context.UserId, conversationKey);

        var message = hadDraft
            ? "Cancelled the company-rule intake and discarded the draft."
            : "There was no company-rule draft in progress.";

        return Task.FromResult(SkillResult.SuccessResult(new { Cancelled = hadDraft }, message));
    }
}
