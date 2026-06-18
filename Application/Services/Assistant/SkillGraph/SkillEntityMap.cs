// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Generic;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

/// <summary>
/// Static curated map from skill name to the primary domain entity (or entities) the skill
/// directly creates, reads, or mutates. Consumed by the substrate-prior deriver to seed
/// co-required skill edges: two skills that operate on the same entity get a grounded prior
/// linking them in the emergent skill-relationship graph. This map is hand-authored (not
/// generated) by design — entity attribution is derived from each skill's injected domain
/// repositories and its ExecuteAsync command/query types, judgements that require human
/// curation rather than reflection. Entity names are constrained to the fixed 15-name
/// vocabulary (Client, Address, Communication, Shift, Work, Break, Contract, Group, GroupItem,
/// AnalyseScenario, ScheduleCommand, WorkChange, Expenses, ClientPeriodHours, Membership);
/// skills whose primary entity falls outside this vocabulary are intentionally absent.
/// </summary>
public static class SkillEntityMap
{
    public static readonly IReadOnlyDictionary<string, string[]> Map = new Dictionary<string, string[]>
    {
        ["accept_scenario"] = new[] { "AnalyseScenario" },
        ["add_break"] = new[] { "Break" },
        ["add_client_email"] = new[] { "Client", "Communication" },
        ["add_client_note"] = new[] { "Client" },
        ["add_client_phone"] = new[] { "Client", "Communication" },
        ["add_client_to_group"] = new[] { "Client", "Group", "GroupItem" },
        ["add_client_to_group_by_name"] = new[] { "Client", "Group", "GroupItem" },
        ["add_expense"] = new[] { "Expenses" },
        ["add_schedule_command"] = new[] { "ScheduleCommand", "Client" },
        ["add_workchange"] = new[] { "WorkChange", "Work", "Client" },
        ["approve_day"] = new[] { "Work", "Break" },
        ["assign_contract_by_name"] = new[] { "Client", "Contract" },
        ["assign_contract_to_client"] = new[] { "Client", "Contract" },
        ["close_period"] = new[] { "Work", "Break" },
        ["confirm_work"] = new[] { "Work" },
        ["cover_absence"] = new[] { "AnalyseScenario", "Work" },
        ["create_contract"] = new[] { "Contract" },
        ["create_employee"] = new[] { "Client", "Address", "Membership" },
        ["create_group"] = new[] { "Group" },
        ["create_shift"] = new[] { "Shift", "Group" },
        ["create_test_environment"] = new[] { "Client", "Address", "Contract", "Group", "GroupItem", "Shift" },
        ["delete_break"] = new[] { "Break" },
        ["delete_client"] = new[] { "Client" },
        ["delete_contract"] = new[] { "Contract" },
        ["delete_expense"] = new[] { "Expenses" },
        ["delete_group"] = new[] { "Group" },
        ["delete_shift"] = new[] { "Shift" },
        ["delete_work"] = new[] { "Work" },
        ["detect_conflicts"] = new[] { "Work" },
        ["email_schedule_to_client"] = new[] { "Client" },
        ["evaluate_scenario"] = new[] { "AnalyseScenario" },
        ["fill_group_by_criteria"] = new[] { "Client", "Group", "GroupItem" },
        ["find_replacement"] = new[] { "Shift" },
        ["generate_period_summary"] = new[] { "Client" },
        ["get_client_details"] = new[] { "Client" },
        ["get_client_locations_overview"] = new[] { "Client", "Address" },
        ["get_contract_details"] = new[] { "Contract" },
        ["get_dashboard_summary"] = new[] { "Shift" },
        ["get_shift_details"] = new[] { "Shift" },
        ["list_client_memberships"] = new[] { "Membership", "Client" },
        ["list_contracts"] = new[] { "Contract" },
        ["list_expenses"] = new[] { "Expenses" },
        ["list_groups"] = new[] { "Group" },
        ["list_groups_hierarchical"] = new[] { "Group" },
        ["list_scenarios"] = new[] { "AnalyseScenario" },
        ["open_schedule"] = new[] { "Group" },
        ["place_work"] = new[] { "Work", "Shift" },
        ["read_schedule_state"] = new[] { "Shift", "Work" },
        ["reject_scenario"] = new[] { "AnalyseScenario" },
        ["remove_client_from_group"] = new[] { "Client", "Group", "GroupItem" },
        ["reopen_period"] = new[] { "Work", "Break" },
        ["revoke_day_approval"] = new[] { "Work", "Break" },
        ["search_employees"] = new[] { "Client" },
        ["search_shifts"] = new[] { "Shift" },
        ["set_group_location"] = new[] { "Group" },
        ["start_autowizard"] = new[] { "Shift", "Group", "Client" },
        ["start_wizard1"] = new[] { "Shift", "Client" },
        ["start_wizard2"] = new[] { "Client" },
        ["start_wizard3"] = new[] { "Client" },
        ["unconfirm_work"] = new[] { "Work" },
        ["update_client"] = new[] { "Client" },
        ["update_client_birthdate"] = new[] { "Client" },
        ["update_client_gender"] = new[] { "Client" },
        ["update_contract"] = new[] { "Contract" },
        ["update_expense"] = new[] { "Expenses" },
        ["update_group"] = new[] { "Group" },
        ["update_membership"] = new[] { "Membership", "Client" },
        ["update_shift"] = new[] { "Shift" },
        ["validate_address"] = new[] { "Address" },
    };
}
