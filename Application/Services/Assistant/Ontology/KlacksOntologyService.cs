// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated world-model for the assistant: a hand-picked seed of the planning-relevant entities +
/// relations + constraints.
/// Generation from the EF DbContext was rejected by design: the schema has 100+ entity types
/// (AppUser, RefreshToken, PostcodeCH, History, ...), and dumping them would bloat the prompt and
/// blow the token budget — the curated ~15-entity subset is the value. The load-bearing part, the
/// constraints, is hand-authored domain semantics that reflection cannot produce.
/// Instead of generating, the curated set is kept honest against the schema by a drift-guard:
/// KlacksOntologyServiceTests asserts internal consistency (every relation/constraint key is a known
/// entity), and an integration test asserts every curated entity name maps to a real EF CLR type.
/// Known limit: the guard catches rename/removal of curated entities, not "a newly relevant entity
/// was not added" — that addition remains a human judgement.
/// </summary>

using System.Text;
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
        ],
        ["Membership"] =
        [
            "Membership.ValidFrom is the planning horizon: a Client is plannable from ValidFrom on, NOT from a contract date (a Client may hold several contracts).",
            "Without an active Membership a Client is not plannable."
        ],
        ["ClientPeriodHours"] =
        [
            "ClientPeriodHours carries an employee's agreed target hours for a period; plans aim to reach (not exceed) that target.",
            "Effective scheduling limits are resolved per client (settings -> contract -> scheduling rule); read them via get_scheduling_defaults / list_scheduling_rules, never assume fixed numbers."
        ],
        ["Expenses"] =
        [
            "Expenses belong to a Work and bill costs (e.g. travel); they are NOT working time and do not count toward hour limits."
        ],
        ["WorkChange"] =
        [
            "WorkChange modifies an existing Work (correction, briefing, travel, replacement) instead of recreating it.",
            "A Replacement WorkChange (ReplaceClientId) covers a Work for another employee and inherits the parent Work's scenario token."
        ],
        ["ScheduleCommand"] =
        [
            "ScheduleCommand is a per-day keyword note on the grid; it carries no working time."
        ]
    };

    public IReadOnlyList<string> GetEntities() => Entities;

    public IReadOnlyList<KlacksOntologyRelation> GetRelations(string entityName)
        => Relations.TryGetValue(entityName, out var list) ? list : [];

    public IReadOnlyList<string> GetConstraints(string entityName)
        => Constraints.TryGetValue(entityName, out var list) ? list : [];

    public string RenderWorldModelBlock(int maxTokens = 1500)
    {
        // [R7/K4] Enforce the token budget (was previously ignored). Truncation happens at whole-entity
        // boundaries — never mid-constraint — and a visible note is appended (not a silent cap), so the
        // block always stays a valid, self-describing document even if it grows past the budget.
        var maxChars = maxTokens * CharsPerToken;

        var body = new StringBuilder(Header);
        var truncated = false;
        foreach (var entity in Entities)
        {
            var entityBlock = new StringBuilder();
            entityBlock.Append('\n').Append("- ").Append(entity);
            foreach (var rel in GetRelations(entity))
            {
                entityBlock.Append('\n').Append("  * ").Append($"{rel.From} --{rel.Kind}--> {rel.To}");
            }
            foreach (var c in GetConstraints(entity))
            {
                entityBlock.Append('\n').Append("  ! ").Append(c);
            }

            // Reserve room so the note + footer can always close the block cleanly.
            var reserve = TruncationNote.Length + Footer.Length + 2;
            if (body.Length + entityBlock.Length + reserve > maxChars)
            {
                truncated = true;
                break;
            }
            body.Append(entityBlock);
        }

        if (truncated)
        {
            body.Append('\n').Append(TruncationNote);
        }
        body.Append('\n').Append(Footer);
        return body.ToString();
    }

    private const int CharsPerToken = 4;
    private const string Header = "=== KLACKS WORLD MODEL ===";
    private const string Footer = "=== END WORLD MODEL ===";
    private const string TruncationNote = "! (world model truncated to fit the token budget)";
}
