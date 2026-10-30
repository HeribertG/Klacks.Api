// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skills whose result or effect depends on the group visibility of the user who runs them: they read or change data
/// that belongs to clients (messenger messages and contacts, received e-mails keyed by the sender address). The
/// visibility guards behind them take the ambient user (HTTP claim, else ExecutionPrincipal) and treat a missing user
/// as an unrestricted background job, which suits pollers and migrations but not a skill call made for a person. A
/// non-admin call of one of these skills with neither a context user nor an ambient owner is therefore refused by the
/// skill executor instead of reaching the guard. Curated by name, like UntrustedSkillOutputs, because the set must also
/// cover plugin skills that have no compile-time descriptor.
/// SkillRestParityGateTests fails when a Supervisor-gated messaging or e-mail skill is neither listed here nor named as
/// independent of group visibility. The inbound-analysis skills (get_messenger_analysis, close_clarification) are listed too:
/// their gate CanManageAutomation reaches the Supervisor, and they answer through the same client visibility guard.
/// diagnose_work_booking is listed because its planner-floor gate reaches every signed-in user while its answer (seals,
/// periods, occupying entries) depends on the client and shift visibility guards. diagnose_planning_period is listed for the
/// same reason: it names an employee's group memberships and day seals and answers through the client visibility guard.
/// evaluate_container_bundling is listed because its planner-floor gate reaches every signed-in user while it names the
/// clients of the services the shift visibility guard lets through. evaluate_employee_exit is listed because its planner-floor
/// gate reaches every signed-in user while it names an employee's bookings and contracts behind the client visibility guard.
/// SkillRestParityGateTests also fails when a skill reachable from the planner floor injects a client or shift visibility guard
/// and is not listed here, which is the reason for the second block of names: their answer or effect passes one of those guards,
/// and a guard without a user answers as for a background job.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GroupVisibilityBoundSkills
{
    public const string UnidentifiedCallerMessage =
        "This action needs a signed-in user whose group visibility applies, and none is attached to this request.";

    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "send_message",
        "send_broadcast",
        "evaluate_messaging_reach",
        "read_messages",
        "list_emails",
        "read_email",
        "get_email_analysis",
        "mark_email_read",
        "move_email_to_folder",
        "delete_email",
        "restore_email",
        "translate_email",
        "list_emails_by_client",
        "list_emails_by_group",
        "get_email_group_tree",
        "get_messenger_analysis",
        "close_clarification",
        "get_replacement_status",
        "diagnose_work_booking",
        "diagnose_planning_period",
        "evaluate_container_bundling",
        "evaluate_employee_exit",
        "add_break",
        "add_break_placeholder",
        "add_schedule_command",
        "add_schedule_commands_range",
        "add_workchange",
        "assign_container_week",
        "check_absence_conflicts",
        "check_client_availability",
        "delete_break_placeholder",
        "detect_conflicts",
        "diagnose_holiday_outcome",
        "email_schedule_to_client",
        "evaluate_absence_hours_setup",
        "evaluate_target_hours_setup",
        "geocode_addresses",
        "get_absence_hours_calculation",
        "get_client_absence_summary",
        "get_client_details",
        "get_target_hours_calculation",
        "list_container_template",
        "list_expiring_contracts",
        "open_schedule",
        "place_work",
        "read_schedule_state",
        "reassign_work",
        "update_break_placeholder"
    };

    public static bool Contains(string? skillName) =>
        !string.IsNullOrEmpty(skillName) && Names.Contains(skillName);

    public static IReadOnlyCollection<string> All => Names;
}
