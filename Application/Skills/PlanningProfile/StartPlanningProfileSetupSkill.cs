// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts a guided custom-planning-profile setup: creates a fresh draft (replacing any setup already in
/// progress) and returns the checklist of parameters to collect. The message instructs the assistant to
/// ask the first question — "what is your business most comparable to?" — offering the five shipped
/// industries as an editable starting base or "scratch" for a blank start, each with the consequence of
/// the choice explained. Draft-only; nothing is persisted until apply.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.PlanningProfile;

[SkillImplementation("start_planning_profile_setup")]
public class StartPlanningProfileSetupSkill : BaseSkillImplementation
{
    private readonly IPendingPlanningProfileDraftStore _draftStore;
    private readonly IPlanningProfileParameterCatalog _catalog;
    private readonly IPlanningProfileDraftValidator _validator;

    public StartPlanningProfileSetupSkill(
        IPendingPlanningProfileDraftStore draftStore,
        IPlanningProfileParameterCatalog catalog,
        IPlanningProfileDraftValidator validator)
    {
        _draftStore = draftStore;
        _catalog = catalog;
        _validator = validator;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = PlanningProfileDraftScope.ConversationKey(context);
        var replaced = _draftStore.Get(context.UserId, conversationKey) is not null;

        var draft = new PlanningProfileDraft();
        _draftStore.Set(context.UserId, conversationKey, draft);

        var checklist = PlanningProfileChecklist.Build(draft, _catalog, _validator);

        var intro = replaced
            ? "Replaced the previous setup. Started a fresh custom-planning-profile setup."
            : "Started a custom-planning-profile setup.";

        var message = intro + " Ask the user one question at a time. Begin with the first question: " +
            "\"What is your business most comparable to?\" Offer the five industries as an editable starting " +
            $"base ({string.Join(", ", IndustrySlugs.All)}) or \"{PlanningProfileBaseChoices.Scratch}\" to start " +
            "blank, and explain the consequence: picking an industry copies all of its scheduling-rule templates " +
            "into the user's own editable rules, while starting blank creates a single empty rule filled entirely " +
            "from the answers that follow. Record the choice with set_planning_profile_parameters (parameter " +
            $"'{PlanningProfileParameterNames.BaseIndustry}'), then work through the optional overrides one by one, " +
            "explaining each parameter's planning impact, before preview_planning_profile and apply_planning_profile.";

        return Task.FromResult(SkillResult.SuccessResult(checklist, message));
    }
}
