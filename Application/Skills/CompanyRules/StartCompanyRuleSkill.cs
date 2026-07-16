// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts a company-rule intake: creates a fresh draft for the given rule kind and the admin's original
/// wording, replacing any draft already in progress, and returns the checklist of required and optional
/// parameters to collect. For a custom-macro rule the compact macro DSL reference is included so the
/// model can author a valid script. Draft-only; nothing is persisted.
/// </summary>
/// <param name="kind">Required. One of surchargeSettings, counterRule, customMacro.</param>
/// <param name="ruleText">Required. The admin's original description of the rule.</param>
/// <param name="name">Optional. A display name for the rule (used in the registry).</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("start_company_rule")]
public class StartCompanyRuleSkill : BaseSkillImplementation
{
    private readonly IPendingCompanyRuleDraftStore _draftStore;
    private readonly ICompanyRuleParameterCatalog _catalog;
    private readonly ICompanyRuleDraftValidator _validator;

    public StartCompanyRuleSkill(
        IPendingCompanyRuleDraftStore draftStore,
        ICompanyRuleParameterCatalog catalog,
        ICompanyRuleDraftValidator validator)
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
        var kindRaw = GetParameter<string>(parameters, "kind");
        if (!CompanyRuleKindParser.TryParse(kindRaw, out var kind, out var kindError))
        {
            return Task.FromResult(SkillResult.Error(kindError!));
        }

        var ruleText = GetParameter<string>(parameters, "ruleText");
        if (string.IsNullOrWhiteSpace(ruleText))
        {
            return Task.FromResult(SkillResult.Error("ruleText is required — pass the admin's original description of the rule."));
        }

        var name = GetParameter<string>(parameters, "name");
        var conversationKey = CompanyRuleDraftScope.ConversationKey(context);
        var replaced = _draftStore.Get(context.UserId, conversationKey) is not null;

        var draft = new CompanyRuleDraft
        {
            Kind = kind,
            RuleText = ruleText.Trim(),
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
        };

        _draftStore.Set(context.UserId, conversationKey, draft);

        var checklist = CompanyRuleChecklist.Build(draft, _catalog, _validator);
        var message = replaced
            ? $"Replaced the previous draft. Started a new {kind} company-rule intake. Collect the required parameters, then preview and apply."
            : $"Started a {kind} company-rule intake. Collect the required parameters, then preview and apply.";

        if (kind == CompanyRuleKind.CustomMacro)
        {
            message += "\n\n" + CompanyRuleMacroDslReference.Reference;
        }

        return Task.FromResult(SkillResult.SuccessResult(checklist, message));
    }
}
