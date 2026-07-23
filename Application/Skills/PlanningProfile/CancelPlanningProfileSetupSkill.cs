// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Cancels the in-progress planning-profile setup by clearing the pending draft. No-op friendly: reports
/// when there was no setup to cancel. Draft-only; nothing is persisted.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.PlanningProfile;

[SkillImplementation("cancel_planning_profile_setup")]
public class CancelPlanningProfileSetupSkill : BaseSkillImplementation
{
    private readonly IPendingPlanningProfileDraftStore _draftStore;

    public CancelPlanningProfileSetupSkill(IPendingPlanningProfileDraftStore draftStore)
    {
        _draftStore = draftStore;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = PlanningProfileDraftScope.ConversationKey(context);
        var hadDraft = _draftStore.Get(context.UserId, conversationKey) is not null;
        _draftStore.Clear(context.UserId, conversationKey);

        var message = hadDraft
            ? "Cancelled the planning-profile setup and discarded the draft."
            : "There was no planning-profile setup in progress.";

        return Task.FromResult(SkillResult.SuccessResult(new { Cancelled = hadDraft }, message));
    }
}
