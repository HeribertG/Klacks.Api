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
        "Client", "Shift", "Work", "Break", "Contract", "Group", "GroupItem",
        "AnalyseScenario", "ScheduleCommand", "WorkChange", "Expenses",
        "ClientPeriodHours", "Membership"
    ];

    private static readonly Dictionary<string, KlacksOntologyRelation[]> Relations = new()
    {
        ["Client"] =
        [
            new("Client", "Contract", "hasMany"),
            new("Client", "Work", "hasMany"),
            new("Client", "Break", "hasMany"),
            new("Client", "GroupItem", "hasMany"),
            new("Client", "ClientPeriodHours", "hasMany")
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
