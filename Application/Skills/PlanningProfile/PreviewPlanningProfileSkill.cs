// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows a full preview of the in-progress planning-profile draft before it is applied: for an industry
/// base it lists every template scheduling rule that will be copied as a customer-owned rule (and states
/// plainly when the industry has zero templates, so the user is never silently moved to custom mode with
/// nothing), for a scratch base it names the single blank rule that will be created; in both cases it
/// shows the field overrides that will be applied and that ACTIVE_INDUSTRIES will be set to the custom
/// marker. When required parameters are still missing it returns the checklist instead. Read-only.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.PlanningProfile;

[SkillImplementation("preview_planning_profile")]
public class PreviewPlanningProfileSkill : BaseSkillImplementation
{
    private readonly IPendingPlanningProfileDraftStore _draftStore;
    private readonly IPlanningProfileParameterCatalog _catalog;
    private readonly IPlanningProfileDraftValidator _validator;
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;

    public PreviewPlanningProfileSkill(
        IPendingPlanningProfileDraftStore draftStore,
        IPlanningProfileParameterCatalog catalog,
        IPlanningProfileDraftValidator validator,
        ISchedulingRuleRepository schedulingRuleRepository)
    {
        _draftStore = draftStore;
        _catalog = catalog;
        _validator = validator;
        _schedulingRuleRepository = schedulingRuleRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var draft = _draftStore.Get(context.UserId, PlanningProfileDraftScope.ConversationKey(context));
        if (draft is null)
        {
            return SkillResult.Error("No planning profile setup in progress. Start one with start_planning_profile_setup first.");
        }

        var missing = _validator.GetMissingRequired(draft);
        if (missing.Count > 0)
        {
            var checklist = PlanningProfileChecklist.Build(draft, _catalog, _validator);
            return SkillResult.SuccessResult(checklist,
                $"Cannot preview yet — still missing required parameter(s): {string.Join(", ", missing)}.");
        }

        var baseChoice = draft.Parameters[PlanningProfileParameterNames.BaseIndustry].Trim().ToLowerInvariant();
        var overrides = CollectOverrides(draft);

        var isScratch = string.Equals(baseChoice, PlanningProfileBaseChoices.Scratch, StringComparison.OrdinalIgnoreCase);

        List<string> rulesToCreate;
        string note;

        if (isScratch)
        {
            var name = draft.Parameters.TryGetValue(PlanningProfileParameterNames.ProfileName, out var explicitName)
                       && !string.IsNullOrWhiteSpace(explicitName)
                ? explicitName.Trim()
                : PlanningProfileDraftDefaults.DefaultScratchRuleName;
            rulesToCreate = new List<string> { name };
            note = $"One new blank customer-owned rule '{name}' will be created from the overrides below.";
        }
        else
        {
            var templates = await _schedulingRuleRepository.GetByIndustryAsync(baseChoice);
            rulesToCreate = templates.Select(t => t.Name).ToList();
            note = rulesToCreate.Count == 0
                ? $"The '{baseChoice}' industry currently has no template scheduling rules, so NO rules will be copied. " +
                  "Only ACTIVE_INDUSTRIES will be set to custom — warn the user that no rules would be created and " +
                  "suggest starting from scratch instead."
                : $"{rulesToCreate.Count} '{baseChoice}' template rule(s) will be copied as new customer-owned rules " +
                  "(fresh rows, industry cleared, import keys cleared — the templates themselves are never changed).";
        }

        var data = new
        {
            BaseChoice = baseChoice,
            IsScratch = isScratch,
            RulesToCreate = rulesToCreate,
            RuleCount = rulesToCreate.Count,
            AppliedOverrides = overrides,
            ActiveIndustriesAfterApply = IndustrySlugs.Custom,
            Note = note
        };

        return SkillResult.SuccessResult(data,
            "Preview of the custom planning profile. " + note +
            " The overrides above apply to every created rule, and ACTIVE_INDUSTRIES becomes 'custom'. " +
            "Review it with the user, then apply_planning_profile to persist it.");
    }

    private Dictionary<string, string> CollectOverrides(PlanningProfileDraft draft)
    {
        var overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in _catalog.GetParameters())
        {
            if (definition.Name == PlanningProfileParameterNames.BaseIndustry
                || definition.Name == PlanningProfileParameterNames.ProfileName)
            {
                continue;
            }

            if (draft.Parameters.TryGetValue(definition.Name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                overrides[definition.Name] = value;
            }
        }

        return overrides;
    }
}
