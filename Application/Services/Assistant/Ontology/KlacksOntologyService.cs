// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated world-model for the assistant: a hand-picked seed of the planning-relevant entities +
/// relations + constraints.
/// Generation from the EF DbContext was rejected by design: the schema has 130+ entity types
/// (AppUser, RefreshToken, PostcodeCH, History, ...), and dumping them would bloat the prompt and
/// blow the token budget — the curated planning subset is the value. The load-bearing part, the
/// constraints, is hand-authored domain semantics that reflection cannot produce.
/// Facts that hold for every entity (soft-delete, scenario scoping, close-vs-delete) live once in
/// GlobalConstraints instead of being repeated per entity.
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
        "Client", "Address", "Communication", "Membership", "Contract", "ClientContract",
        "Shift", "ContainerTemplate", "Work", "WorkChange", "Break", "Absence",
        "Group", "GroupItem", "AnalyseScenario", "ScheduleCommand", "ScheduleNote",
        "Expenses", "ShiftExpenses", "SurchargeItem",
        "ClientPeriodHours", "SchedulingRule", "PeriodCapRule", "Period",
        "ClientAvailability", "ClientShiftPreference", "SealedDay", "WorkSoftening",
        "Qualification", "ClientQualification", "ShiftRequiredQualification",
        "CalendarSelection", "SelectedCalendar", "CalendarRule", "CompanyRule"
    ];

    private static readonly string[] GlobalConstraints =
    [
        "Almost every entity below is soft-deleted: a delete only sets IsDeleted, rows never vanish. Never assume a row is gone. Exceptions (hard delete): SurchargeItem, CalendarRule.",
        "AnalyseToken scopes a row to a scenario: NULL = real schedule, set = that scenario only. Never mix the two.",
        "An object with history is CLOSED (set an end date), never deleted — delete is blocked by any existing work.",
        "Klacks is multilingual: fields marked MultiLanguage below are per-language objects (core de/en/fr/it, more via language packs), NOT plain strings. Never compare, sort or write them as a bare string — always resolve the user's language first, and when creating one fill at least the core languages.",
        "Answer in the language the user writes in, independent of the language a catalog text happens to be stored in."
    ];

    private static readonly Dictionary<string, KlacksOntologyRelation[]> Relations = new()
    {
        ["Client"] =
        [
            new("Client", "Address", "hasMany"),
            new("Client", "Communication", "hasMany"),
            new("Client", "Membership", "hasOne"),
            new("Client", "ClientContract", "hasMany"),
            new("Client", "Work", "hasMany"),
            new("Client", "Break", "hasMany"),
            new("Client", "GroupItem", "hasMany"),
            new("Client", "ClientPeriodHours", "hasMany"),
            new("Client", "ClientAvailability", "hasMany"),
            new("Client", "ClientQualification", "hasMany"),
            new("Client", "ClientShiftPreference", "hasMany")
        ],
        ["Address"] =
        [
            new("Address", "Client", "belongsTo")
        ],
        ["Communication"] =
        [
            new("Communication", "Client", "belongsTo")
        ],
        ["ClientContract"] =
        [
            new("ClientContract", "Client", "belongsTo"),
            new("ClientContract", "Contract", "belongsTo")
        ],
        ["Contract"] =
        [
            new("Contract", "CalendarSelection", "optionallyBelongsTo")
        ],
        ["Shift"] =
        [
            new("Shift", "Work", "hasMany"),
            new("Shift", "GroupItem", "hasMany"),
            new("Shift", "ShiftExpenses", "hasMany"),
            new("Shift", "ShiftRequiredQualification", "hasMany"),
            new("Shift", "ContainerTemplate", "hasMany"),
            new("Shift", "Shift", "cutInto")
        ],
        ["Work"] =
        [
            new("Work", "Client", "belongsTo"),
            new("Work", "Shift", "belongsTo"),
            new("Work", "WorkChange", "hasMany"),
            new("Work", "Expenses", "hasMany"),
            new("Work", "SurchargeItem", "hasMany"),
            new("Work", "AnalyseScenario", "optionallyBelongsTo")
        ],
        ["Break"] =
        [
            new("Break", "Client", "belongsTo"),
            new("Break", "Absence", "belongsTo"),
            new("Break", "Work", "optionallyBelongsTo")
        ],
        ["Group"] =
        [
            new("Group", "Group", "parentOf"),
            new("Group", "Client", "containsViaGroupItem"),
            new("Group", "Shift", "containsViaGroupItem")
        ],
        ["GroupItem"] =
        [
            new("GroupItem", "Group", "belongsTo"),
            new("GroupItem", "Client", "optionallyBelongsTo"),
            new("GroupItem", "Shift", "optionallyBelongsTo")
        ],
        ["ClientQualification"] =
        [
            new("ClientQualification", "Client", "belongsTo"),
            new("ClientQualification", "Qualification", "belongsTo")
        ],
        ["ShiftRequiredQualification"] =
        [
            new("ShiftRequiredQualification", "Shift", "belongsTo"),
            new("ShiftRequiredQualification", "Qualification", "belongsTo")
        ],
        ["PeriodCapRule"] =
        [
            new("PeriodCapRule", "SchedulingRule", "optionallyBelongsTo")
        ],
        ["CalendarSelection"] =
        [
            new("CalendarSelection", "SelectedCalendar", "hasMany")
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
        ["Communication"] =
        [
            "Communication is one contact channel of exactly one Client: Type + Prefix + Value (max 100 chars).",
            "CommunicationTypeEnum is NOT contiguous — 0=PrivateFixPhone, 1=PrivateCellPhone, 2=OfficeFixPhone, 3=OfficeCellPhone, 4=PrivateMail, 5=OfficeMail, 6=Others, 7=EmergencyPhone. Never infer a number.",
            "Email lives in types 4/5 only; a Client without one is unreachable for the email pipeline."
        ],
        ["Membership"] =
        [
            "Membership.ValidFrom is the planning horizon: a Client is plannable from ValidFrom on, NOT from a contract date (a Client may hold several contracts).",
            "Without an active Membership a Client is not plannable."
        ],
        ["Contract"] =
        [
            "Contract carries working conditions and the holiday calendar (Feiertagsregelung).",
            "Contracts apply ONLY to employees (Employee/ExternEmp), assigned via client_contract.",
            "Without a client_contract the settings default contract is used — acceptable only for simple plans, otherwise discouraged."
        ],
        ["ClientContract"] =
        [
            "ClientContract assigns one Contract to one Client for [FromDate..UntilDate]; UntilDate NULL means open-ended.",
            "IsActive marks the assignment currently in force; a Client may hold several assignments over time."
        ],
        ["Shift"] =
        [
            "Shift.ShiftType: 0=IsTask (a single service), 1=IsContainer (a container bundling services via ContainerTemplate).",
            "Shift.Status lifecycle: 0=OriginalOrder (editable) -> seal -> 1=SealedOrder (immutable snapshot, the order/'Bestellung') -> clone -> 2=OriginalShift (plannable) -> cut -> N x 3=SplitShift.",
            "End state is SealedOrder + exactly ONE OriginalShift, or SealedOrder + N SplitShifts — never both; an OriginalShift alongside its own splits double-books.",
            "Cut tree: OriginalId always points at the SealedOrder; ParentId/RootId carry the cut hierarchy (RootId conventions differ between seeded and freshly cut trees — never rely on RootId alone).",
            "FromDate/UntilDate bound the period the Shift may be planned in; the weekday flags (IsMonday..IsSunday, IsHoliday, IsWeekdayAndHoliday) decide on which days it recurs.",
            "For a sporadic Shift (IsSporadic) placing a Work is REJECTED twice over: when the employees engaged that day reach EffectiveSumEmployees (day capacity), and when the booked days inside the sporadic range reach EffectiveQuantity (range capacity). SporadicScope: 0=Week, 1=Month, 2=Quarter, 3=Semester, 4=Year, 5=ContractualTerm.",
            "Deleting a Shift is blocked while ANY Work references it, including purely historical ones."
        ],
        ["ContainerTemplate"] =
        [
            "ContainerTemplate is one weekday slot of a container Shift (FromTime/UntilTime, Weekday, holiday flags) plus its transport mode and route.",
            "Only a Shift with ShiftType=IsContainer carries templates."
        ],
        ["Work"] =
        [
            "Work with LockLevel != None is immutable for Wizards 1+2+3.",
            "Work with IsGroupRestricted=true (cross-group sealed) is read-only in the current view but must remain visible to prevent double-bookings.",
            "Work.Workday must lie within Client.Contract.[StartDate..EndDate]."
        ],
        ["WorkChange"] =
        [
            "WorkChange modifies an existing Work (correction, briefing, travel, replacement) instead of recreating it.",
            "A Replacement WorkChange (ReplaceClientId) covers a Work for another employee and inherits the parent Work's scenario token."
        ],
        ["Break"] =
        [
            "Break is the actual absence EVENT on a client's day; it always references an Absence type (AbsenceId).",
            "Break.Description is MultiLanguage (stored as one jsonb column, unlike the other MultiLanguage fields).",
            "Break is always immutable for Wizards (semantic, regardless of LockLevel).",
            "Break.WorkTime counts toward TargetHours but NOT toward MaxWeeklyHours.",
            "A Break interrupts a MaxConsecutiveDays sequence like a free day."
        ],
        ["Absence"] =
        [
            "Absence is the CATALOG of absence types (holiday, sickness, ...), not an absence itself — the event is Break.",
            "Name, Description and Abbreviation are MultiLanguage — an absence type is looked up by its text in the user's language, never by a single hard-coded string.",
            "Absence flags drive the calculation: WithHoliday/WithSaturday/WithSunday decide which days count, IsUnpaid marks unpaid types, AppliesToContainer allows use on container shifts."
        ],
        ["Group"] =
        [
            "Groups give structure: both employees (Employee/ExternEmp) and Customers can be grouped via GroupItem.",
            "Deleting a Group is blocked by ANY planned work in the group or its whole subtree; closing it (ValidUntil) is blocked only by works after that date."
        ],
        ["GroupItem"] =
        [
            "GroupItem is the membership join to a Group (GroupId mandatory); it links either a Client or a Shift, ClientId/ShiftId are optional.",
            "ValidFrom/ValidUntil bound the membership window — schedule cells outside it are not plannable, on top of the Membership limit.",
            "Deleting a membership is strict (blocked by any work, even historical); closing it via ValidUntil is future-relative (blocked only by works after that date)."
        ],
        ["AnalyseScenario"] =
        [
            "Accepting a scenario merges its scenario-bound works into main schedule (analyse_token becomes NULL).",
            "Rejecting a scenario soft-deletes both the scenario row and its bound works."
        ],
        ["ScheduleCommand"] =
        [
            "ScheduleCommand is a per-day keyword note on the grid; it carries no working time."
        ],
        ["ScheduleNote"] =
        [
            "ScheduleNote is free text on one client's day (ClientId + CurrentDate); it carries no working time and no keyword semantics."
        ],
        ["Expenses"] =
        [
            "Expenses belong to a Work and bill costs (e.g. travel); they are NOT working time and do not count toward hour limits.",
            "Taxable=true is an expense (salary component), Taxable=false a reimbursement."
        ],
        ["ShiftExpenses"] =
        [
            "ShiftExpenses is the expense default defined ON the Shift; the booked amount on a day is Expenses."
        ],
        ["SurchargeItem"] =
        [
            "SurchargeItem is a computed surcharge amount attached to exactly one of Work, Break or WorkChange; it is derived, never planned by hand."
        ],
        ["ClientPeriodHours"] =
        [
            "ClientPeriodHours carries an employee's agreed target hours for a period; plans aim to reach (not exceed) that target.",
            "Effective scheduling limits are resolved per client (settings -> contract -> scheduling rule); read them via get_scheduling_defaults / list_scheduling_rules, never assume fixed numbers."
        ],
        ["SchedulingRule"] =
        [
            "SchedulingRule holds the named limit set (MaxWorkDays, MinRestDays, MinPauseHours, MaxDailyHours, MaxWeeklyHours, MaxConsecutiveDays, night/holiday/weekend rates, ...).",
            "Every limit is nullable — a NULL means 'not restricted by this rule', not zero."
        ],
        ["PeriodCapRule"] =
        [
            "PeriodCapRule caps the hours of a period (Period/Scope + CapHours, optional rolling window and MaxAverageWeeklyHours); it may hang off a SchedulingRule or stand alone."
        ],
        ["Period"] =
        [
            "Period is one accounting period: [FromDate..UntilDate] with FullHours as its target volume."
        ],
        ["ClientAvailability"] =
        [
            "ClientAvailability is POSITIVE and resolved PER DAY: (ClientId, Date, Hour 0-23, IsAvailable). No record on a day = the whole day is open.",
            "As soon as one IsAvailable=true record exists for a day, ONLY those hours are usable — the unmarked hours are blocked.",
            "Precedence for a cell: Break > ScheduleCommand keyword > availability. Availability is a hard veto for all autofill wizards."
        ],
        ["ClientShiftPreference"] =
        [
            "ClientShiftPreference records a Client's preference for or against a Shift; it steers the planners but is not a hard constraint."
        ],
        ["SealedDay"] =
        [
            "SealedDay seals a date (optionally for one Group) at a WorkLockLevel and records who sealed it and why — a sealed day is closed for planning."
        ],
        ["WorkSoftening"] =
        [
            "WorkSoftening records that a named rule was deliberately relaxed for one client-day; it documents the exception, it does not remove the rule."
        ],
        ["Qualification"] =
        [
            "Qualification is the catalog of skills/certificates, optionally country-scoped and time-limited (IsTimeLimited).",
            "Name and Description are MultiLanguage."
        ],
        ["ClientQualification"] =
        [
            "ClientQualification is a Qualification held by a Client with a Level and an optional validity window (ValidFrom/ValidUntil) — an expired one no longer qualifies."
        ],
        ["ShiftRequiredQualification"] =
        [
            "ShiftRequiredQualification demands a Qualification at MinLevel for a Shift; IsMandatory=true means an employee without it must not be planned."
        ],
        ["CalendarSelection"] =
        [
            "CalendarSelection is a named set of holiday calendars; Contract.CalendarSelectionId points at one to resolve which days are holidays. It is nullable — without it the contract has no holiday rules of its own."
        ],
        ["SelectedCalendar"] =
        [
            "SelectedCalendar is one country/state entry of a CalendarSelection; OfficialOverride forces a day's official-holiday status against the imported default."
        ],
        ["CalendarRule"] =
        [
            "CalendarRule is one holiday rule of a country/state; Rule/SubRule define WHEN it falls, IsMandatory whether it is official and IsPaid whether it is paid.",
            "Name and Description are MultiLanguage — a holiday is never identified by a single-language string."
        ],
        ["CompanyRule"] =
        [
            "CompanyRule is a company policy in plain text plus the parameters derived from it, targeted at an entity type/id. Applying it WRITES the target entity (settings, scheduling rule, macro) — the planners enforce those written values, never the rule text itself."
        ]
    };

    public IReadOnlyList<string> GetEntities() => Entities;

    public IReadOnlyList<KlacksOntologyRelation> GetRelations(string entityName)
        => Relations.TryGetValue(entityName, out var list) ? list : [];

    public IReadOnlyList<string> GetConstraints(string entityName)
        => Constraints.TryGetValue(entityName, out var list) ? list : [];

    public string RenderWorldModelBlock(int maxTokens = IKlacksOntologyService.DefaultMaxTokens)
    {
        // [R7/K4] Enforce the token budget (was previously ignored). Truncation happens at whole-entity
        // boundaries — never mid-constraint — and a visible note is appended (not a silent cap), so the
        // block always stays a valid, self-describing document even if it grows past the budget.
        var maxChars = maxTokens * CharsPerToken;

        var body = new StringBuilder(Header);
        var truncated = false;

        // The cross-cutting rules are rendered once instead of per entity, but they are optional: on a
        // budget too small to hold them the per-entity facts win.
        var globalBlock = new StringBuilder();
        foreach (var rule in GlobalConstraints)
        {
            globalBlock.Append('\n').Append("! ").Append(rule);
        }

        if (body.Length + globalBlock.Length + ReserveLength <= maxChars)
        {
            body.Append(globalBlock);
        }

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
            if (body.Length + entityBlock.Length + ReserveLength > maxChars)
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
    private static readonly int ReserveLength = TruncationNote.Length + Footer.Length + 2;
}
