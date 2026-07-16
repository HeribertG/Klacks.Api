// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows a full preview of the in-progress company-rule draft before it is applied: for SurchargeSettings
/// the old-to-new comparison of every affected setting, for CounterRule the resolved scheduling-rule
/// scope, for CustomMacro the script plus the macro-script validation result. When required parameters
/// are still missing it returns the checklist instead of a preview. Read-only; nothing is persisted.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Handlers.CompanyRules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("preview_company_rule")]
public class PreviewCompanyRuleSkill : BaseSkillImplementation
{
    private readonly IPendingCompanyRuleDraftStore _draftStore;
    private readonly ICompanyRuleParameterCatalog _catalog;
    private readonly ICompanyRuleDraftValidator _validator;
    private readonly ISettingsReader _settingsReader;
    private readonly IMacroScriptValidator _macroScriptValidator;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IMediator _mediator;

    public PreviewCompanyRuleSkill(
        IPendingCompanyRuleDraftStore draftStore,
        ICompanyRuleParameterCatalog catalog,
        ICompanyRuleDraftValidator validator,
        ISettingsReader settingsReader,
        IMacroScriptValidator macroScriptValidator,
        IComplianceEnforcementResolver enforcementResolver,
        IMediator mediator)
    {
        _draftStore = draftStore;
        _catalog = catalog;
        _validator = validator;
        _settingsReader = settingsReader;
        _macroScriptValidator = macroScriptValidator;
        _enforcementResolver = enforcementResolver;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var draft = _draftStore.Get(context.UserId, CompanyRuleDraftScope.ConversationKey(context));
        if (draft is null)
        {
            return SkillResult.Error("No company rule draft in progress. Start one with start_company_rule first.");
        }

        var missing = _validator.GetMissingRequired(draft);
        if (missing.Count > 0)
        {
            var checklist = CompanyRuleChecklist.Build(draft, _catalog, _validator);
            return SkillResult.SuccessResult(checklist,
                $"Cannot preview yet — still missing required parameter(s): {string.Join(", ", missing)}.");
        }

        object detail = draft.Kind switch
        {
            CompanyRuleKind.SurchargeSettings => await BuildSurchargePreviewAsync(draft),
            CompanyRuleKind.CounterRule => await BuildCounterRulePreviewAsync(draft, cancellationToken),
            CompanyRuleKind.CustomMacro => BuildCustomMacroPreview(draft),
            _ => new { }
        };

        var data = new
        {
            Kind = draft.Kind.ToString(),
            draft.RuleText,
            draft.Name,
            Detail = detail
        };

        return SkillResult.SuccessResult(data,
            $"Preview of the {draft.Kind} company rule. Review it, then apply_company_rule to persist it.");
    }

    private async Task<object> BuildSurchargePreviewAsync(CompanyRuleDraft draft)
    {
        var changes = new List<object>();
        foreach (var (parameterName, settingKey) in CompanyRuleSurchargeSettingMap.ParameterToSettingKey)
        {
            if (!draft.Parameters.TryGetValue(parameterName, out var newValue) || string.IsNullOrWhiteSpace(newValue))
            {
                continue;
            }

            var existing = await _settingsReader.GetSetting(settingKey);
            changes.Add(new
            {
                Parameter = parameterName,
                SettingKey = settingKey,
                OldValue = existing?.Value,
                NewValue = newValue
            });
        }

        return new { Changes = changes };
    }

    private async Task<object> BuildCounterRulePreviewAsync(CompanyRuleDraft draft, CancellationToken cancellationToken)
    {
        string scope;
        if (draft.Parameters.TryGetValue(CompanyRuleParameterNames.SchedulingRuleName, out var ruleName) && !string.IsNullOrWhiteSpace(ruleName))
        {
            var rules = (await _mediator.Send(new Queries.ListQuery<SchedulingRuleResource>(), cancellationToken) ?? []).ToList();
            var (rule, error) = SchedulingRuleResolver.Resolve(rules, ruleName);
            scope = rule is not null ? $"Scheduling rule '{rule.Name}' ({rule.Id})" : error!;
        }
        else
        {
            scope = "Global (all clients)";
        }

        var enforcementValue = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.Enforcement);
        var globalMode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.CounterRule);
        var enforcementNote = BuildEnforcementNote(enforcementValue, globalMode);

        return new
        {
            EventType = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.EventType),
            Period = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.Period),
            Threshold = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.Threshold),
            HoursThreshold = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.HoursThreshold),
            Enforcement = enforcementValue,
            EnforcementNote = enforcementNote,
            Scope = scope
        };
    }

    private static string? BuildEnforcementNote(string? enforcementValue, RuleEnforcementMode globalMode)
    {
        if (string.IsNullOrWhiteSpace(enforcementValue)
            || !Enum.TryParse<RuleEnforcementMode>(enforcementValue, ignoreCase: true, out var chosenMode))
        {
            return null;
        }

        return chosenMode == globalMode
            ? $"Matches the current global counter-rule mode ({globalMode})."
            : $"Override active for this rule only: {chosenMode} instead of the current global mode ({globalMode}).";
    }

    private object BuildCustomMacroPreview(CompanyRuleDraft draft)
    {
        var script = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.MacroScript) ?? string.Empty;
        var validation = _macroScriptValidator.Validate(script);

        return new
        {
            MacroName = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.MacroName),
            Category = draft.Parameters.GetValueOrDefault(CompanyRuleParameterNames.MacroCategory) ?? nameof(MacroCategoryEnum.Shift),
            Script = script,
            ScriptValid = validation.IsValid,
            ScriptError = validation.IsValid ? null : validation.ErrorMessage
        };
    }
}
