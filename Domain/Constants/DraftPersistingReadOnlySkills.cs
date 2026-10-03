// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single source of truth for skills that SkillRiskClassifier classifies ReadOnly (so the autonomy gate
/// lets them run unconfirmed) although they persist rows: create_plan stores a draft plan in agent_plans and
/// issues a confirmation token, and the company-rule and planning-profile intake steps upsert or delete the
/// owner's draft row (PersistentPendingCompanyRuleDraftStore, PersistentPendingPlanningProfileDraftStore).
/// ReadOnly here means "needs no confirmation", not "writes nothing". Consumers that need the stronger
/// guarantee - a read-only personal access token (McpReadModeToolPolicy) and the read-only research sub-loop
/// (ReadOnlyToolsetFilter) - exclude this set; SkillRiskClassifier.ReadOnlyExtras includes it.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class DraftPersistingReadOnlySkills
{
    public const string StartCompanyRule = "start_company_rule";
    public const string SetCompanyRuleParameters = "set_company_rule_parameters";
    public const string CancelCompanyRule = "cancel_company_rule";
    public const string StartPlanningProfileSetup = "start_planning_profile_setup";
    public const string SetPlanningProfileParameters = "set_planning_profile_parameters";
    public const string CancelPlanningProfileSetup = "cancel_planning_profile_setup";

    public static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        PlanSkillDefaults.CreatePlanSkillName,
        StartCompanyRule,
        SetCompanyRuleParameters,
        CancelCompanyRule,
        StartPlanningProfileSetup,
        SetPlanningProfileParameters,
        CancelPlanningProfileSetup
    };

    public static bool Contains(string skillName) => Names.Contains(skillName);
}
