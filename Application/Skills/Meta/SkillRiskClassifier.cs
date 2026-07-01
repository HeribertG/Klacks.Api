// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies skills into risk classes for autonomy gating. Order: explicit sensitive list,
/// scenario-gated writers (mutations land in an AnalyseScenario that a human accepts),
/// reversible skills (InverseSkillRegistry mapping or explicit extras), read-only detection
/// (explicit allow-list, then read-only categories; a read-only name prefix is ignored for
/// write categories so a write skill cannot bypass the gate via its name), everything else
/// defaults to irreversible.
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
        "delete_identity_provider",
        // Destructive, cascading or hard-to-undo structural deletes: require explicit confirmation
        // even at the Autonomous default level (deleting a whole org unit cascades to its shifts,
        // deleting a client removes a person and their data, deleting a membership shifts the
        // plannability boundary). These are rare, so the confirmation friction is low.
        "delete_group",
        "delete_branch",
        "delete_client",
        "delete_membership",
        // PAT self-management must stay exclusive to the JWT-authenticated REST endpoint
        // (PersonalAccessTokensController) — otherwise a PAT- or OAuth-authenticated MCP
        // session could mint itself a fresh token, defeating revocation.
        "create_personal_access_token"
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

    // Skills whose names carry a read-only prefix but whose category is a write category (Crud).
    // They genuinely only read (list candidates), so they are allow-listed explicitly instead of
    // being trusted via the name prefix — which a future write skill could abuse.
    private static readonly HashSet<string> ReadOnlyExtras = new(StringComparer.OrdinalIgnoreCase)
    {
        "find_customer_candidates",
        "find_split_shift_candidates"
    };

    private static readonly HashSet<SkillCategory> ReadOnlyCategories =
    [
        SkillCategory.Query,
        SkillCategory.Read,
        SkillCategory.Validation,
        SkillCategory.UI
    ];

    // Categories that always mutate state: a read-only name prefix must NOT classify them as
    // read-only (closes the trap where e.g. a Crud skill named "evaluate_*" would bypass the gate).
    private static readonly HashSet<SkillCategory> WriteCategories =
    [
        SkillCategory.Crud,
        SkillCategory.Action
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
        if (ReadOnlyExtras.Contains(descriptor.Name))
        {
            return true;
        }

        if (ReadOnlyCategories.Contains(descriptor.Category))
        {
            return true;
        }

        if (WriteCategories.Contains(descriptor.Category))
        {
            return false;
        }

        return ReadOnlyNamePrefixes.Any(prefix =>
            descriptor.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
