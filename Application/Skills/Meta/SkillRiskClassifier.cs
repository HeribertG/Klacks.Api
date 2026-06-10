// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies skills into risk classes for autonomy gating. Order: explicit sensitive list,
/// scenario-gated writers (mutations land in an AnalyseScenario that a human accepts),
/// reversible skills (InverseSkillRegistry mapping or explicit extras), read-only detection
/// via category and name prefix, everything else defaults to irreversible.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Meta;

public class SkillRiskClassifier : ISkillRiskClassifier
{
    private const string ManualInverseMarker = "__manual__";

    private static readonly HashSet<string> SensitiveSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete_system_user",
        "assign_user_permissions",
        "set_user_group_scope",
        "set_autonomy_level",
        "create_identity_provider",
        "update_identity_provider",
        "delete_identity_provider"
    };

    private static readonly HashSet<string> ScenarioGatedSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "propose_plan",
        "start_autowizard",
        "start_wizard1",
        "start_wizard2",
        "start_wizard3",
        "cover_absence"
    };

    private static readonly HashSet<string> ReversibleExtras = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete_work",
        "delete_break",
        "cancel_wizard_job"
    };

    private static readonly HashSet<SkillCategory> ReadOnlyCategories =
    [
        SkillCategory.Query,
        SkillCategory.Validation,
        SkillCategory.UI
    ];

    private static readonly string[] ReadOnlyNamePrefixes =
    [
        "get_", "list_", "search_", "find_", "read_", "lookup_", "verify_",
        "check_", "detect_", "interpret_", "validate_", "test_", "evaluate_", "generate_"
    ];

    public SkillRiskClass Classify(SkillDescriptor descriptor)
    {
        if (SensitiveSkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.Sensitive;
        }

        if (ScenarioGatedSkills.Contains(descriptor.Name))
        {
            return SkillRiskClass.ScenarioGated;
        }

        if (IsReversible(descriptor.Name))
        {
            return SkillRiskClass.Reversible;
        }

        if (IsReadOnly(descriptor))
        {
            return SkillRiskClass.ReadOnly;
        }

        return SkillRiskClass.Irreversible;
    }

    private static bool IsReversible(string skillName)
    {
        if (ReversibleExtras.Contains(skillName))
        {
            return true;
        }

        return InverseSkillRegistry.TryGet(skillName, out var inverse)
            && !string.Equals(inverse.SkillName, ManualInverseMarker, StringComparison.Ordinal);
    }

    private static bool IsReadOnly(SkillDescriptor descriptor)
    {
        if (ReadOnlyCategories.Contains(descriptor.Category))
        {
            return true;
        }

        return ReadOnlyNamePrefixes.Any(prefix =>
            descriptor.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
