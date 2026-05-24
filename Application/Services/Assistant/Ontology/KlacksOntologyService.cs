// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 3 skeleton implementation of IKlacksOntologyService.
/// Today: hard-coded seed of the most important entities + relations + constraints.
/// TODO (next iteration): build the graph from EF DbContext reflection + .claude/rules/*.md
/// + DK ontology entries; refresh on migration completion.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Ontology;

public class KlacksOntologyService : IKlacksOntologyService
{
    private static readonly string[] Entities =
    [
        "Client", "Address", "Communication", "Shift", "Work", "Break", "Contract", "Group", "GroupItem",
        "AnalyseScenario", "ScheduleCommand", "WorkChange", "Expenses",
        "ClientPeriodHours", "Membership"
    ];

    private static readonly Dictionary<string, KlacksOntologyRelation[]> Relations = new()
    {
        ["Client"] =
        [
            new("Client", "Address", "hasMany"),
            new("Client", "Communication", "hasMany"),
            new("Client", "Membership", "hasOne"),
            new("Client", "Contract", "hasMany"),
            new("Client", "Work", "hasMany"),
            new("Client", "Break", "hasMany"),
            new("Client", "GroupItem", "hasMany"),
            new("Client", "ClientPeriodHours", "hasMany")
        ],
        ["Address"] =
        [
            new("Address", "Client", "belongsTo")
        ],
        ["Communication"] =
        [
            new("Communication", "Client", "belongsTo")
        ],
        ["Shift"] =
        [
            new("Shift", "Work", "hasMany"),
            new("Shift", "GroupItem", "hasMany")
        ],
        ["Work"] =
        [
            new("Work", "Client", "belongsTo"),
            new("Work", "Shift", "belongsTo"),
            new("Work", "AnalyseScenario", "optionallyBelongsTo")
        ],
        ["Group"] =
        [
            new("Group", "Group", "parentOf"),
            new("Group", "Client", "containsViaGroupItem"),
            new("Group", "Shift", "containsViaGroupItem")
        ]
    };

    private static readonly Dictionary<string, string[]> Constraints = new()
    {
        ["Client"] =
        [
            "Client.EntityType: 0=Employee, 1=ExternEmp (external employee), 2=Customer.",
            "Plans are created for Employee/ExternEmp on behalf of a Customer.",
            "A Client is never created alone: minimum is Client + Address + Communication + Membership.",
            "No Client without an Address and a Membership — both are mandatory.",
            "Communication (phone + email) is strongly recommended: no email => no planning via email, and email lets the client write to Klacks directly."
        ],
        ["Address"] =
        [
            "Address always belongs to exactly one Client (clientId).",
            "Address requires street, zip, city, state and country; state+country must be filled (ADDRESS_COMPLETENESS).",
            "Address.Type (AddressTypeEnum): 0=Employee (employee's address), 1=Workplace (Customer address), 2=InvoicingAddress (Customer address).",
            "Employees never have an InvoicingAddress; Workplace and InvoicingAddress are Customer addresses.",
            "Addresses are time-versioned by ValidFrom: the in-scope address is the newest with ValidFrom <= reference date.",
            "Before saving, an address should be geo-validated (validate_address, openrouteservice); on failure offer suggestions or force-save."
        ],
        ["Contract"] =
        [
            "Contract carries working conditions and the holiday calendar (Feiertagsregelung).",
            "Contracts apply ONLY to employees (Employee/ExternEmp), assigned via client_contract.",
            "Without a client_contract the settings default contract is used — acceptable only for simple plans, otherwise discouraged."
        ],
        ["Group"] =
        [
            "Groups give structure: both employees (Employee/ExternEmp) and Customers can be grouped via GroupItem."
        ],
        ["Work"] =
        [
            "Work with LockLevel != None is immutable for Wizards 1+2+3.",
            "Work with IsGroupRestricted=true (cross-group sealed) is read-only in the current view but must remain visible to prevent double-bookings.",
            "Work.Workday must lie within Client.Contract.[StartDate..EndDate]."
        ],
        ["Break"] =
        [
            "Break is always immutable for Wizards (semantic, regardless of LockLevel).",
            "Break.WorkTime counts toward TargetHours but NOT toward MaxWeeklyHours."
        ],
        ["AnalyseScenario"] =
        [
            "Accepting a scenario merges its scenario-bound works into main schedule (analyse_token becomes NULL).",
            "Rejecting a scenario soft-deletes both the scenario row and its bound works."
        ]
    };

    public IReadOnlyList<string> GetEntities() => Entities;

    public IReadOnlyList<KlacksOntologyRelation> GetRelations(string entityName)
        => Relations.TryGetValue(entityName, out var list) ? list : [];

    public IReadOnlyList<string> GetConstraints(string entityName)
        => Constraints.TryGetValue(entityName, out var list) ? list : [];

    public string RenderWorldModelBlock(int maxTokens = 1500)
    {
        var lines = new List<string> { "=== KLACKS WORLD MODEL ===" };
        foreach (var entity in Entities)
        {
            lines.Add($"- {entity}");
            foreach (var rel in GetRelations(entity))
            {
                lines.Add($"  * {rel.From} --{rel.Kind}--> {rel.To}");
            }
            foreach (var c in GetConstraints(entity))
            {
                lines.Add($"  ! {c}");
            }
        }
        lines.Add("=== END WORLD MODEL ===");
        return string.Join('\n', lines);
    }
}
