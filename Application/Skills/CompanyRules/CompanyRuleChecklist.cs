// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the intake checklist shown to the admin for a company-rule draft: which required parameters are
/// still open, which optional parameters may still be set, and which values are already collected. Shared
/// by start_company_rule and set_company_rule_parameters so both describe the draft the same way.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleChecklist
{
    public static object Build(
        CompanyRuleDraft draft,
        ICompanyRuleParameterCatalog catalog,
        ICompanyRuleDraftValidator validator)
    {
        var missing = validator.GetMissingRequired(draft);
        var missingSet = new HashSet<string>(missing, StringComparer.Ordinal);
        var definitions = catalog.GetParameters(draft.Kind);

        var requiredOpen = definitions
            .Where(d => missingSet.Contains(d.Name))
            .Select(Describe)
            .ToList();

        var optionalOpen = definitions
            .Where(d => !missingSet.Contains(d.Name) && !draft.Parameters.ContainsKey(d.Name))
            .Select(Describe)
            .ToList();

        var provided = draft.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => p.Key, p => p.Value);

        return new
        {
            Kind = draft.Kind.ToString(),
            Complete = missing.Count == 0,
            MissingRequired = missing,
            RequiredOpen = requiredOpen,
            OptionalOpen = optionalOpen,
            Provided = provided
        };
    }

    private static object Describe(Klacks.Api.Domain.Models.Settings.CompanyRuleParameterDefinition definition) => new
    {
        definition.Name,
        definition.Description,
        DataType = definition.DataType.ToString(),
        definition.Required,
        EnumValues = definition.EnumValues,
        definition.Min,
        definition.Max
    };
}
