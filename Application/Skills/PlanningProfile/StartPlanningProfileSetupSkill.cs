// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts a guided custom-planning-profile setup: creates a fresh draft (replacing any setup already in
/// progress) and returns the checklist of parameters to collect. The message instructs the assistant to
/// ask the first question — "what is your business most comparable to?" — offering the five shipped
/// industries as an editable starting base or "scratch" for a blank start, each with the consequence of
/// the choice explained. Draft-only; nothing is persisted until apply.
/// </summary>
/// <param name="baseIndustry">
/// Optional. The base choice, when the caller already collected it — the guided recipe asks for it
/// before starting the setup. Accepting it here keeps that hand-off deterministic: the alternative,
/// letting the model relay the answer into set_planning_profile_parameters, depends on the model
/// building a JSON object correctly, and the recipe engine cannot inject one (inject carries plain
/// strings). An invalid value is reported and the setup still starts, so the assistant can simply ask
/// again instead of losing the draft.
/// </param>

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
        var baseIndustryOutcome = TryApplyBaseIndustry(draft, parameters);
        _draftStore.Set(context.UserId, conversationKey, draft);

        var checklist = PlanningProfileChecklist.Build(draft, _catalog, _validator);

        var intro = replaced
            ? "Replaced the previous setup. Started a fresh custom-planning-profile setup."
            : "Started a custom-planning-profile setup.";

        if (baseIndustryOutcome is not null)
        {
            return Task.FromResult(SkillResult.SuccessResult(checklist, intro + " " + baseIndustryOutcome));
        }

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

    /// <summary>
    /// Stores a base choice supplied by the caller. Returns the follow-up instruction for the assistant,
    /// or null when no value was passed at all — then the caller asks the base question itself.
    /// </summary>
    private string? TryApplyBaseIndustry(PlanningProfileDraft draft, Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue(PlanningProfileParameterNames.BaseIndustry, out var raw))
        {
            return null;
        }

        var value = raw?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var validation = _validator.Validate(PlanningProfileParameterNames.BaseIndustry, value);
        if (!validation.IsValid)
        {
            return $"The base choice '{value}' was rejected: {validation.ErrorMessage} " +
                "Ask the user again which of the shipped industries their business is most comparable to, " +
                $"or \"{PlanningProfileBaseChoices.Scratch}\" for a blank start, and record it with " +
                "set_planning_profile_parameters.";
        }

        draft.Parameters[PlanningProfileParameterNames.BaseIndustry] = value;

        return $"The base choice '{value}' is recorded. Now work through the optional overrides one by one " +
            "with set_planning_profile_parameters, explaining each parameter's meaning and its planning " +
            "impact, and ask for exactly one value at a time. The checklist states each parameter's type " +
            "and its valid range — when you ask for a number, use the number-field reply format with the " +
            "min and max from the checklist. When the user is done, call preview_planning_profile and then " +
            "apply_planning_profile.";
    }
}
