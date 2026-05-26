// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Skills.Meta;

/// <summary>
/// Phase 5 — read-only map of "skill X is rolled back by skill Y with these param mappings".
/// Skills not in the map are NOT auto-reversible and must escalate to HITL.
/// </summary>
public static class InverseSkillRegistry
{
    /// <summary>
    /// Lookup: original-skill-name → (inverse-skill-name, param-mapping-hint).
    /// Param-mapping-hint is informational — the actual rollback skill must take the right id
    /// from the original execution's result.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, InverseSkill> Map =
        new Dictionary<string, InverseSkill>(StringComparer.OrdinalIgnoreCase)
        {
            ["place_work"] = new("delete_work", "Take workId from the original execution's result."),
            ["add_break"] = new("delete_break", "Take breakId from the original execution's result."),
            ["confirm_work"] = new("unconfirm_work", "Same workId."),
            ["unconfirm_work"] = new("confirm_work", "Same workId."),
            ["approve_day"] = new("revoke_day_approval", "Same date + groupId."),
            ["revoke_day_approval"] = new("approve_day", "Same date + groupId."),
            ["close_period"] = new("reopen_period", "Same startDate + endDate."),
            ["reopen_period"] = new("close_period", "Same startDate + endDate."),
            ["accept_scenario"] = new("__manual__", "Accepting merges scenario into main; rollback is manual or requires a fresh scenario producer."),
            ["reject_scenario"] = new("__manual__", "Rejected scenarios cannot be revived — the data is soft-deleted."),
            ["add_client_to_group"] = new("__manual__", "remove_client_from_group skill TODO."),
            ["create_branch"] = new("delete_branch", "Take branchId from the original execution's result."),
            ["create_employee"] = new("__manual__", "delete_employee / mark inactive skill TODO."),
            ["create_user"] = new("delete_system_user", "Take userId."),
            ["create_shift"] = new("__manual__", "delete_shift skill TODO."),
            ["add_schedule_command"] = new("__manual__", "delete_schedule_command skill TODO."),
            ["start_autowizard"] = new("cancel_wizard_job", "Cancels in-flight jobs only; produced scenarios stay until accepted or rejected."),
            ["start_wizard1"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["start_wizard2"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["start_wizard3"] = new("cancel_wizard_job", "Cancels in-flight job."),
            ["add_ai_memory"] = new("delete_ai_memory", "Take memoryId.")
        };

    public static bool TryGet(string skillName, out InverseSkill inverse)
        => Map.TryGetValue(skillName, out inverse!);
}

public sealed record InverseSkill(string SkillName, string ParamHint);
