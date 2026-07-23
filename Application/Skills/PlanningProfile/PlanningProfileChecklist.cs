// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the setup checklist shown to the admin for a planning-profile draft: which required parameters
/// are still open, which optional parameters may still be set, and which values are already collected.
/// Each open parameter carries its meaning and its planning impact so the assistant can ask for one value
/// at a time and explain what the answer will change. Shared by start_planning_profile_setup and
/// set_planning_profile_parameters so both describe the draft the same way.
/// </summary>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Skills.PlanningProfile;

internal static class PlanningProfileChecklist
{
    public static object Build(
        PlanningProfileDraft draft,
        IPlanningProfileParameterCatalog catalog,
        IPlanningProfileDraftValidator validator)
    {
        var missing = validator.GetMissingRequired(draft);
        var missingSet = new HashSet<string>(missing, StringComparer.Ordinal);
        var definitions = catalog.GetParameters();

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
            Complete = missing.Count == 0,
            MissingRequired = missing,
            RequiredOpen = requiredOpen,
            OptionalOpen = optionalOpen,
            Provided = provided
        };
    }

    private static object Describe(PlanningProfileParameterDefinition definition) => new
    {
        definition.Name,
        definition.Description,
        definition.PlanningImpact,
        DataType = definition.DataType.ToString(),
        definition.Required,
        EnumValues = definition.EnumValues,
        definition.Min,
        definition.Max
    };
}
